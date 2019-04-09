using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Digital_Player_Adv : ScriptableObject {
    protected float[][] network_values;
    private int UUID = 0, par1_UUID = -1, par2_UUID = -1;
    private int top_position = -1, start_generation = -1, last_generation = -1, top_generation = -1;
    private float top_score = 0;

    private static int[] sizes = {32 * 3, 32, 32, 32 + 4};
    private static int LAST_UUID = -1;

    //  Create a player with the default values
    public Digital_Player_Adv() {
        network_values = new float[sizes.Length - 1][];
        for (int i = 0; i < sizes.Length - 1; i++) {
            int local_size = (sizes[i] + 1) * sizes[i + 1];
            network_values[i] = new float[local_size];
            for (int j = 0; j < local_size; j++) {
                network_values[i][j] = 1;
            }
        }
    }

    //  Assign the UUID
    public void OnEnable() {
        LAST_UUID++;
        UUID = LAST_UUID;
    }

    //  Copy from a single player
    public Digital_Player_Adv(Digital_Player_Adv copy) {
        network_values = new float[sizes.Length - 1][];
        for (int i = 0; i < sizes.Length - 1; i++) {
            network_values[i] = (float[]) copy.network_values[i].Clone();
        }
    }

    //  Get the value for a board state
    public float GetBoardTotalValue(List<float> input) {
        int FRIEND_MAN = 32, ENEMY_MAN = 33, FRIEND_KING = 34, ENEMY_KING = 35;
        float[] parms = GetBoardValues(input);
        float score_sum = 0;
        for (int i = 0; i < 32; i++) {
            int inp_point = i * 3;
            if (input[inp_point] == 1) {
                //  Has piece
                float pos_score = parms[i];
                if (input[inp_point + 1] == 0) {
                    //  Its friendly
                    if (input[inp_point + 2] == 0) { score_sum += parms[FRIEND_MAN] * pos_score; }
                    else { score_sum += parms[FRIEND_KING] * pos_score; }
                }   else {
                    if (input[inp_point + 2] == 0) { score_sum -= parms[ENEMY_MAN] * pos_score; }
                    else { score_sum -= parms[ENEMY_KING] * pos_score; }
                }
            }
        }
        return score_sum;
    }

    //  Get the list of values for a board state
    public float[] GetBoardValues(List<float> main_input) {
        List<float> input = new List<float>(main_input);
        List<float> output = new List<float>();
        for (int input_row = 0; input_row < sizes.Length - 1; input_row++) {
            input.Add(1);
            output.Clear();
            for (int out_node = 0; out_node < sizes[input_row + 1]; out_node++) {
                float node_sum = 0;
                for (int in_node = 0; in_node < input.Count; in_node++) {
                    int net_index = out_node * input.Count + in_node;
                    node_sum += input[in_node] * network_values[input_row][net_index];
                }
                node_sum = Activation(node_sum);
                output.Add(node_sum);
            }
            input = new List<float>(output);
        }
        return output.ToArray();
    }

    //  Randomize the player
    public void Randomize() {
        for (int i = 0; i < sizes.Length - 1; i++) {
            int local_size = (sizes[i] + 1) * sizes[i + 1];
            network_values[i] = new float[local_size];
            for (int j = 0; j < local_size; j++) {
                network_values[i][j] = Random.Range(-1f, 1f); ;
            }
        }
    }

    //  The activation function 
    private float Activation(float x) {
        return (float)System.Math.Tanh(x);
    }

    //  Inherit from two different parents
    public void Inherit(Digital_Player_Adv par1, Digital_Player_Adv par2, float weight) {
        par1_UUID = par1.GetUUID();
        par2_UUID = par2.GetUUID();

        for (int i = 0; i < sizes.Length - 1; i++) {
            int local_size = (sizes[i] + 1) * sizes[i + 1];
            network_values[i] = new float[local_size];
            for (int j = 0; j < local_size; j++) {
                if (Random.Range(0f, 1f) > weight) {
                    //  Copy parent 1
                    network_values[i][j] = par1.network_values[i][j];
                }   else {
                    //  Copy parent 2
                    network_values[i][j] = par2.network_values[i][j];
                }
            }
        }
    }

    //  Tweak the values randomly
    public void MutateValues(float mutate_chance, float max_mutation) {
        for (int i = 0; i < sizes.Length - 1; i++) {
            int local_size = (sizes[i] + 1) * sizes[i + 1];
            network_values[i] = new float[local_size];
            for (int j = 0; j < local_size; j++) {
                if (Random.Range(0f, 1f) < mutate_chance) {
                    float val = network_values[i][j];
                    if (Mathf.Abs(val) < 0.1) { val = 0.1f * Mathf.Sign(val); }

                    float mut = Random.Range(-1f, 1f) * max_mutation * val;
                    network_values[i][j] += mut;
                    network_values[i][j] = Mathf.Clamp(network_values[i][j], -1000, 1000);
                }
            }
        }
    }

    //  Update Stats
    public void UpdateHighscore(int position, float score, int generation) {
        if (start_generation == -1) { start_generation = generation; }
        last_generation = generation;
        //  Scores
        if ((top_position == -1) || (top_position < position)) {
            top_position = position;
            top_score = score;
            top_generation = generation;
        }
        //  Generation
    }

    //  Returns the UUID
    public int GetUUID() {
        return UUID;
    }

    //  Get the text for display
    public string GetGenUUID() {
        string o = UUID.ToString();
        if (par1_UUID != -1) {
            o += "  (";
            if (par1_UUID == par2_UUID) { o += par1_UUID; }
            else { o += par1_UUID + " + " + par2_UUID; }
            o += ")";
        }
        return o;
    }

    //  Get the string for storage
    public string GetStorageString() {
        string o = "";
        string nl = System.Environment.NewLine;
        //  UUIDs
        o += UUID.ToString() + nl;
        o += par1_UUID.ToString() + nl;
        o += par2_UUID.ToString() + nl;
        //  Scores
        o += top_position.ToString() + nl;
        o += top_score.ToString() + nl;
        //  Generation
        o += start_generation.ToString() + nl;
        o += top_generation.ToString() + nl;
        o += last_generation.ToString() + nl;

        for (int i = 0; i < sizes.Length - 1; i++) {
            int local_size = (sizes[i] + 1) * sizes[i + 1];
            for (int j = 0; j < local_size; j++) {
                o += network_values[i][j].ToString() + nl;
            }
        }

        return o;
    }

    //  Load from the storage string
    public void LoadFromString(List<string> input) {
        int l = 8;
        for (int i = 0; i < sizes.Length - 1; i++) {
            int local_size = (sizes[i] + 1) * sizes[i + 1];
            for (int j = 0; j < local_size; j++) {
                network_values[i][j] = float.Parse(input[l]);
                l++;
            }
        }

    }
}
