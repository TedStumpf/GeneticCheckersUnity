using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class Active_Board : MonoBehaviour {
    public Transform board_render_class;
    private Transform board_render;
    private Board_Render board_render_script;
    private Board_Headless game_board;
    private Digital_Player_Adv p1, p2;
    private Vector2 selected_tile = new Vector2(-1, -1);
    private bool auto = false;

    // Use this for initialization
    void Start() {
        p1 = ScriptableObject.CreateInstance<Digital_Player_Adv>();
        p2 = ScriptableObject.CreateInstance<Digital_Player_Adv>();

        p1.Randomize();

        game_board = new Board_Headless(p1, p2);

        board_render = Instantiate(board_render_class);
        board_render.parent = transform;
        board_render.transform.localPosition = new Vector3(-70, -70, 0);
        board_render.transform.localScale = Vector3.one;
        board_render_script = board_render.GetComponent<Board_Render>();

        UpdateDisplay(true);
    }

    //  Handle the input when a user clicks on a tile
    public void PlayerClick(GameObject source) {
        if (game_board.GetPlayerTurn() == Board_Headless.PLAYER_BLACK) {
            Board_Tile_Contol btc = source.GetComponent<Board_Tile_Contol>();
            Vector2 clicked = new Vector2(btc.x, btc.y);

            if (selected_tile == clicked) {
                selected_tile = new Vector2(-1, -1);
            }   else {
                //  Get vaild moves
                List<Vector2> vaild_moves = new List<Vector2>();
                List<List<int>> moves = game_board.get_all_valid_moves();
                for (int i = 0; i < moves.Count; i++) {
                    Vector2 start = new Vector2(moves[i][0], moves[i][1]);
                    if (selected_tile.x == -1) {
                        vaild_moves.Add(start);
                    }
                    else {
                        if (start == selected_tile) {
                            Vector2 post = new Vector2(moves[i][2], moves[i][3]);
                            vaild_moves.Add(post);
                        }
                    }
                }

                if (vaild_moves.Contains(clicked)) {
                    if (selected_tile.x == -1) {
                        selected_tile = clicked;
                    }   else {
                        if (vaild_moves.Contains(clicked)) {
                            List<int> move = new List<int>();
                            move.Add((int)selected_tile.x);
                            move.Add((int)selected_tile.y);
                            move.Add((int)clicked.x);
                            move.Add((int)clicked.y);
                            game_board.make_move(move);
                            selected_tile = clicked;
                            if (game_board.GetPlayerTurn() == Board_Headless.PLAYER_WHITE) {
                                selected_tile = new Vector2(-1, -1);
                            }
                        }
                    }
                }
            }
            UpdateDisplay(true);
        }
    }

    //  Have the AI take its turn
    public void Update() {
        if (auto && (game_board.GetPlayerTurn() == Board_Headless.PLAYER_WHITE)) {
            AI_Turn();
        }
    }

    //  Load an AI from a text file
    public void LoadAI() {
        string path = EditorUtility.OpenFilePanel("Load AI", "", "txt");
        StreamReader reader = new StreamReader(path);
        List<string> input = new List<string>();
        string line = reader.ReadLine();
        while (line != null) {
            input.Add(line);
            line = reader.ReadLine();
        }
        reader.Close();
        p1.LoadFromString(input);
        UpdateDisplay(true);
    }

    //  Switch the current players turn
    public void SwitchPlayers() {
        game_board.switch_player();
        UpdateDisplay(true);
    }

    //  Toggle the AI autmatic turns
    public void ToggleAuto() {
        auto = !auto;
    }

    //  Take the AI's turn
    public void AI_Turn() {
        if (game_board.GetPlayerTurn() == Board_Headless.PLAYER_WHITE) {
            Board_Headless.ValueMovePair move = game_board.get_best_move(7, Board_Headless.PLAYER_WHITE);
            game_board.make_all_moves(move.move);
            UpdateDisplay(true);
        }
    }

    //  Update the display
    private void UpdateDisplay(bool AI_hint) {
        float[] ai_data = null;
        if (AI_hint) {
            ai_data = p1.GetBoardValues(game_board.GetOneHotArray(Board_Headless.PLAYER_WHITE));
        }

        List<Vector2> vaild_moves = new List<Vector2>();
        if (game_board.GetPlayerTurn() == Board_Headless.PLAYER_BLACK) {
            List<List<int>> moves = game_board.get_all_valid_moves();
            for (int i = 0; i < moves.Count; i++) {
                Vector2 start = new Vector2(moves[i][0], moves[i][1]);
                if (selected_tile.x == -1) {
                    vaild_moves.Add(start);
                }   else {
                    if (start == selected_tile) {
                        Vector2 post = new Vector2(moves[i][2], moves[i][3]);
                        vaild_moves.Add(post);
                    }
                }
            }
        }

        for (int x = 0; x < 8; x++) {
            for (int y = 0; y < 8; y++) {
                if ((x + y) % 2 == 0) {
                    int valid = 0;
                    Vector2 pos_arr = new Vector2(x, y);

                    if (vaild_moves.Contains(pos_arr)) {
                        valid = 1;
                    }
                    if (pos_arr == selected_tile) {
                        valid = 2;
                    }

                    if (AI_hint) {
                        float p = ai_data[game_board.cords_to_index(x, y)];
                        board_render.GetComponent<Board_Render>().Update_Tile(x, y, game_board.get_board_tile(x, y) % 2, p, game_board.is_king(x, y), valid);
                    }   else {
                        board_render.GetComponent<Board_Render>().Update_Tile(x, y, game_board.get_board_tile(x, y) % 2, 0, game_board.is_king(x, y), valid);
                    }
                }
            }
        }
    }
}

