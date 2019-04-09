using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using CielaSpike;

public class Overseer : MonoBehaviour {
    public int population_size, games_per_player;
    public float win_score = 1, lose_score = -0.9f, draw_score = -0.1f, speed_bonus = 0.2f;
    public float top_keep_perc = 0.1f, random_keep_perc = 0.1f, new_player_perc = 0.2f;
    public OverseerPanelManager display;
    private int generation = 0, next_game = 0, max_tasks = 5;
    private PopulationState population_status = PopulationState.Ungenerated;
    private List<PlayerScorePair> population;
    private PlayerScorePair best_lastgen_player;
    private List<int[]> tournament_games;
    private List<Task> running_tasks;
    private bool run_all = false, run_once = false, run_repeat = false;
    private string start_time;

    private enum PopulationState {
        Ungenerated,
        Running,
        WaitForComplete,
        Completed,
        Sorted
    }
    

    // Use this for initialization
    void Start () {
        population = new List<PlayerScorePair>();
        running_tasks = new List<Task>();
        for (int i = 0; i < population_size; i++) {
            Digital_Player_Adv dp = ScriptableObject.CreateInstance<Digital_Player_Adv>();
            dp.Randomize();
            PlayerScorePair pair = new PlayerScorePair(dp, 0);
            population.Add(pair);
        }

        start_time = ((long)(System.DateTime.UtcNow - (new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc))).TotalMilliseconds).ToString();
        UpdateDisplay();
	}
	
	// Update is called once per frame
	void Update () {
        if (run_repeat) { run_once = true; }

        switch (population_status) {
            case PopulationState.Ungenerated:
                SetUpTournament();
                SaveGeneration();
                break;
            case PopulationState.Running:
            case PopulationState.WaitForComplete:
                UpdateRunningWait();
                break;
            case PopulationState.Completed:
                if (run_once) {
                    SortPopulation();
                    SaveGeneration();
                }
                break;
            case PopulationState.Sorted:
                if (run_once) { GenerateNextGeneration(); }
                break;
        }
        run_once = false;

        UpdateDisplay();
    }

    //  Saves the current generation
    private void SaveGeneration() {
        string folder = start_time;
        //  Main folder
        string folder_path = Path.Combine(Application.persistentDataPath, folder);
        if (!Directory.Exists(folder_path)) {
            Directory.CreateDirectory(folder_path);
        }
        //  Data
        for (int i = 0; i < population.Count; i++) {
            population[i].digital_player.UpdateHighscore(i, population[i].score, generation);
            string file_name = population[i].digital_player.GetUUID() + ".txt";
            string data_path = Path.Combine(folder_path, file_name);

            using (StreamWriter streamWriter = File.CreateText(data_path)) {
                streamWriter.Write(population[i].digital_player.GetStorageString());
            }
        }
    }

    //  Manage the tasks until completion
    private void UpdateRunningWait() {
        //  Clear tasks
        for (int i = 0; i < running_tasks.Count;) {
            if (running_tasks[i].State == TaskState.Done) {
                running_tasks.RemoveAt(i);
            } else {
                i++;
            }
        }

        //  Clear Wait
        if ((population_status == PopulationState.WaitForComplete) && (running_tasks.Count == 0)) {
            population_status = PopulationState.Completed;
        }

        //  Run next tasks
        if (run_all || run_once) {
            if ((population_status == PopulationState.Running) || (population_status == PopulationState.WaitForComplete)) {
                DoBoardRun();
            }   else {
                run_all = false;
            }
        }
        run_once = false;
    }

    //  Update the display
    private void UpdateDisplay() {
        string status_text = "";
        string task_text = "";
        switch (population_status) {
            case PopulationState.Ungenerated:
                status_text = "Status: Waiting for game list";

                display.SetButtonTextRun();
                break;
            case PopulationState.Running:
            case PopulationState.WaitForComplete:
                int total = tournament_games.Count;
                int completed = next_game - running_tasks.Count;
                status_text = "Status: Running (" + completed + " / " + total + ")  " + (int)((100 * completed) / total) + "%";
                status_text += "    Gen #" + generation;
                task_text = "Running Tasks: " + running_tasks.Count;
                display.SetButtonTextRun();
                break;
            case PopulationState.Completed:
                status_text = "Status: Complete";
                display.SetButtonTextSort();
                break;
            case PopulationState.Sorted:
                status_text = "Status: Sorted";
                display.SetButtonTextNext();
                break;
        }
        display.DisplayUpdateStatus(status_text);
        display.DisplayUpdateTasks(task_text);
        if (best_lastgen_player == null) { display.DisplayUpdateScore("Top Score: None"); }
        else { display.DisplayUpdateScore("Top Score: " + best_lastgen_player.score + "  (" + best_lastgen_player.digital_player.GetGenUUID() + ")"); }
    }

    //  Create the tournament matches
    private void SetUpTournament() {
        if (population_status == PopulationState.Ungenerated) {
            tournament_games = new List<int[]>();
            for (int p = 0; p < population_size; p++) {
                List<int> cont = new List<int>();
                cont.Add(p);
                while (cont.Count <= games_per_player) {
                    int n = Random.Range(0, population_size);
                    if (!cont.Contains(n)) {
                        tournament_games.Add(new int[] { p, n });
                        cont.Add(n);
                    }
                }
            }
            population_status = PopulationState.Running;
            next_game = 0;
        }
    }

    //  Sort the population
    private void SortPopulation() {
        if (population_status == PopulationState.Completed) {
            population.Sort((x, y) => x.CompareScore(y));
            population.Reverse();
            population_status = PopulationState.Sorted;
        }
    }

    //  Generate the next generation
    private void GenerateNextGeneration() {
        List<PlayerScorePair> next_generation = new List<PlayerScorePair>();
        List<int> selected_pops = new List<int>();

        int top_keep = (int) (population_size * top_keep_perc);
        int random_keep = (int)(population_size * random_keep_perc);
        int new_perc = (int)(population_size * new_player_perc);

        //  Top keep
        for (int i = 0; i < top_keep; i++) {
            PlayerScorePair new_pair = new PlayerScorePair(population[i].digital_player, 0);
            next_generation.Add(new_pair);
            selected_pops.Add(i);
        }

        //  Random keep
        for (int i = 0; i < random_keep; i++) {
            int selected = Random.Range(0, population_size);
            if (!selected_pops.Contains(selected)) {
                PlayerScorePair new_pair = new PlayerScorePair(population[selected].digital_player, 0);
                next_generation.Add(new_pair);
                selected_pops.Add(selected);
            }
        }

        //  New players
        for (int i = 0; i < new_perc; i++) {
            Digital_Player_Adv dp = ScriptableObject.CreateInstance<Digital_Player_Adv>();
            dp.Randomize();
            PlayerScorePair pair = new PlayerScorePair(dp, 0);
            next_generation.Add(pair);
        }

        //  Merging players
        while (next_generation.Count < population_size) {
            int player_id_1 = (int)(population_size * (Random.Range(0f, 1f) * Random.Range(0f, 1f)));
            int player_id_2 = (int)(population_size * (Random.Range(0f, 1f) * Random.Range(0f, 1f)));
            while (player_id_1 == player_id_2) {
                player_id_2 = (int)(population_size * (Random.Range(0f, 1f) * Random.Range(0f, 1f)));
            }
            Digital_Player_Adv new_player = ScriptableObject.CreateInstance <Digital_Player_Adv>();
            new_player.Inherit(population[player_id_1].digital_player, population[player_id_2].digital_player, Random.Range(0f, 1f));
            new_player.MutateValues(Random.Range(0f, 0.5f), Random.Range(0f, 0.2f));
            next_generation.Add(new PlayerScorePair(new_player, 0));
        }

        //  Reset vars
        best_lastgen_player = population[0];
        Debug.Log("Best Player for gen " + generation + ": " + best_lastgen_player.digital_player.GetGenUUID());
        Debug.Log("Best Player for gen " + generation + " score: " + best_lastgen_player.score);
        //display.UpdateBoard(best_lastgen_player.digital_player);
        population = next_generation;
        tournament_games.Clear();
        population_status = PopulationState.Ungenerated;
        generation += 1;
        next_game = 0;
    }

    //  Run a single game
    public void RunOnce() {
        if (population_status == PopulationState.Ungenerated) { SetUpTournament(); }
        run_once = true;
    }

    //  Run all the games
    public void RunAll() {
        if (population_status == PopulationState.Ungenerated) { SetUpTournament(); }
        run_all = !run_all;
    }

    //  Run multiple generations
    public void RunRepeat() {
        if (population_status == PopulationState.Ungenerated) { SetUpTournament(); }
        run_repeat = !run_repeat;
    }

    //  Start running a board for the game
    private void DoBoardRun() {
        if ((next_game < tournament_games.Count) && (running_tasks.Count < max_tasks)) {
            int p1_id = tournament_games[next_game][0];
            int p2_id = tournament_games[next_game][1];
            PlayerScorePair p1 = population[p1_id];
            PlayerScorePair p2 = population[p2_id];
            Board_Headless board = new Board_Headless(p1.digital_player, p2.digital_player);
            next_game++;
            /// MP Section
            Task task = new Task(ExecuteBoardRun(board, p1_id));
            running_tasks.Add(task);
            this.StartCoroutineAsync(task);
            /// END MP
            if (next_game >= tournament_games.Count) { population_status = PopulationState.WaitForComplete; }
        }
    }

    //  Execution for a single game
    IEnumerator ExecuteBoardRun(Board_Headless board, int p1_id) {
        board.run_all();
        int winner = board.get_winner();
        //  Player 1 is white
        PlayerScorePair p1 = population[p1_id];
        if (winner == Board_Headless.PLAYER_WHITE) { p1.score += win_score + board.GetMovePercentage(15, 45) * speed_bonus; }
        else if (winner == Board_Headless.PLAYER_BLACK) { p1.score += lose_score; }
        else if (winner == Board_Headless.PLAYER_DRAW) { p1.score += draw_score; }
        yield break;
    }

    //  A class that groups a player and its score
    private class PlayerScorePair {
        public Digital_Player_Adv digital_player;
        public float score;

        public PlayerScorePair(Digital_Player_Adv dp, float s) {
            digital_player = dp;
            score = s;
        }

        public int CompareScore(PlayerScorePair other) {
            if (score < other.score) return -1;
            if (score > other.score) return 1;
            return 0;
        }
    }
}
