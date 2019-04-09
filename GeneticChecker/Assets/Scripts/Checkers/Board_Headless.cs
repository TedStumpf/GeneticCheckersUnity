using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CielaSpike;

public class Board_Headless {
    private List<int> board;
    private const int CHECKER_NONE = -1, CHECKER_WHITE = 0, CHECKER_BLACK = 1, CHECKER_WHITE_KING = 2, CHECKER_BLACK_KING = 3;
    public const int PLAYER_NONE = -1, PLAYER_WHITE = 0, PLAYER_BLACK = 1, PLAYER_DRAW = 2;
    private int TIMEOUT_LIMIT = 30;
    private int player_turn, turns_until_timeout, total_moves;
    private float preview_time = 0;
    private Vector2 forced_move;
    private Digital_Player_Adv[] digital_players;

    // Use this for initialization
    public Board_Headless(Digital_Player_Adv p1, Digital_Player_Adv p2) {
        turns_until_timeout = TIMEOUT_LIMIT;
        total_moves = 0;
        forced_move = new Vector2(-1, -1);
        //  Create board and set to empty
        board = new List<int>(32);
        for (int i = 0; i < 32; i++) { board.Add(CHECKER_NONE); }
        //  Set defualt checker positions
        for (int x = 0; x < 8; x += 2) {
            for (int y = 0; y < 3; y++) {
                set_board_tile(x + y % 2, y, CHECKER_BLACK);
                set_board_tile(x + (y + 5) % 2, y + 5, CHECKER_WHITE);
            }
        }
        //  Load default players
        digital_players = new Digital_Player_Adv[2] { p1, p2 };
        //  Pick a random player
        player_turn = UnityEngine.Random.Range(0, 2);
    }

    //  Runs the game to completion
    public int run_all() {
        while (get_winner() == PLAYER_NONE) {
            run_step();
        }
        return get_winner();
    }

    //  Runs a single turn in the game
    private void run_step() {
        ValueMovePair move = get_best_move((((float)turns_until_timeout) / TIMEOUT_LIMIT > 0.5) ? 6 : 3, player_turn);
        if (move.move.Count == 0) {
            //  No moves, timeout game
            turns_until_timeout = -1;
        }
        else {
            make_all_moves(move.move);
        }
    }

    //  Executes all moves in the provided list
    public void make_all_moves(List<int> moves) {
        turns_until_timeout -= 1;
        total_moves += 1;
        for (int i = 0; i < moves.Count; i += 4) {
            set_board_tile(moves[i + 2], moves[i + 3], get_board_tile(moves[i], moves[i + 1]));
            if (((is_white(moves[i + 2], moves[i + 3]) && (moves[i + 3] == 0)) || (is_black(moves[i + 2], moves[i + 3]) && (moves[i + 3] == 7))) && !is_king(moves[i + 2], moves[i + 3])) {
                set_board_tile(moves[i + 2], moves[i + 3], get_board_tile(moves[i + 2], moves[i + 3]) + 2);
            }
            set_board_tile(moves[i], moves[i + 1], CHECKER_NONE);
            if (is_jump(moves, i)) {
                turns_until_timeout = TIMEOUT_LIMIT;
                set_board_tile((moves[i] + moves[i + 2]) / 2, (moves[i + 1] + moves[i + 3]) / 2, CHECKER_NONE);
            }
        }
        switch_player();
    }

    //  Executes a single move
    public void make_move(List<int> move) {
        set_board_tile(move[2], move[3], get_board_tile(move[0], move[1]));
        if (((is_white(move[2], move[3]) && (move[3] == 0)) || (is_black(move[2], move[3]) && (move[3] == 7))) && !is_king(move[2], move[3])) {
            set_board_tile(move[2], move[3], get_board_tile(move[2], move[3]) + 2);
        }
        set_board_tile(move[0], move[1], CHECKER_NONE);
        if (is_jump(move)) {
            set_board_tile((move[0] + move[2]) / 2, (move[1] + move[3]) / 2, CHECKER_NONE);
            forced_move.Set(move[2], move[3]);
            turns_until_timeout = TIMEOUT_LIMIT;
            if (get_all_valid_moves().Count == 0) {
                switch_player();
            }
        }   else {
            switch_player();
        }
    }

    //  Returns the best move and its score
    private ValueMovePair get_best_move(int itr_depth, float alpha, float beta, bool max_white, int player) {
        //  Base Case
        if ((get_winner() != PLAYER_NONE) || (itr_depth == 0)) {
            return new ValueMovePair(score_board(player) * (player == PLAYER_WHITE ? 1 : -1), null);
        }

        // Recursive Case
        List<List<int>> moves = get_all_expanded_moves();
        List<int> board_backup = new List<int>(board);
        int player_backup = player_turn;
        int timeout_backup = turns_until_timeout;
        int total_backup = total_moves;
        Vector2 forced_backup = forced_move;
        List<ValueMovePair> results = new List<ValueMovePair>();
        float best_score = max_white ? -1000000 : 1000000;
        for (int i = 0; i < moves.Count; i++) {
            //  Get the best follow up move
            make_all_moves(moves[i]);
            ValueMovePair best_sub_move = get_best_move(itr_depth - 1, alpha, beta, !max_white, player);
            best_sub_move.move = moves[i];
            results.Add(best_sub_move);
            //  Restore Backup
            board = new List<int>(board_backup);
            player_turn = player_backup;
            turns_until_timeout = timeout_backup;
            total_moves = total_backup;
            forced_move = forced_backup;
            //  Prune
            if (max_white) {
                best_score = Mathf.Max(best_score, best_sub_move.value);
                alpha = Mathf.Max(best_score, alpha);
            }
            else {
                best_score = Mathf.Min(best_score, best_sub_move.value);
                beta = Mathf.Min(best_score, beta);
            }
            if (alpha >= beta) { return best_sub_move; }
        }

        //  Default return
        for (int i = 0; i < results.Count;) {
            if (results[i].value != best_score) {
                results.RemoveAt(i);
            }
            else { i++; }
        }
        if (results.Count > 0) {
            return results[0];
        }
        return new ValueMovePair(best_score, new List<int>());
    }

    //  Returns the best move and its score
    public ValueMovePair get_best_move(int itr_depth, int player) {
        return get_best_move(itr_depth, float.NegativeInfinity, float.PositiveInfinity, player == PLAYER_WHITE, player);
    }

    //  Returns a list of all of the vaild moves
    public List<List<int>> get_all_valid_moves() {
        List<List<int>> output = new List<List<int>>();
        if (forced_move.x != -1) {
            output = get_all_valid_moves_for_pos((int)forced_move.x, (int)forced_move.y);
            if (output == null) { output = new List<List<int>>(); }
        }
        else {
            for (int x = 0; x < 8; x++) {
                for (int y = 0; y < 8; y++) {
                    List<List<int>> moves = get_all_valid_moves_for_pos(x, y);
                    if (moves != null) { output.AddRange(moves); }
                }
            }
        }
        //  Filter out jump moves
        List<List<int>> jump_moves = new List<List<int>>();
        for (int i = 0; i < output.Count; i++) {
            List<int> move = output[i];
            if (is_jump(move)) { jump_moves.Add(move); }
        }
        if ((jump_moves.Count > 0) || (forced_move.x != -1)) { output = jump_moves; }

        //  Return
        return output;
    }

    //  Returns the list of all expanded vaild moves
    private List<List<int>> get_all_expanded_moves() {
        return expand_moves(get_all_valid_moves());
    }

    //  Expand the list of moves
    private List<List<int>> expand_moves(List<List<int>> basic_moves) {
        //  Expand moves to completion
        List<List<int>> output = new List<List<int>>();
        //  Backup settings
        List<int> board_backup = new List<int>(board);
        int player_backup = player_turn;
        int turn_backup = turns_until_timeout;
        Vector2 forced_backup = forced_move;
        //  Expand the remaining moves
        for (int i = 0; i < basic_moves.Count; i++) {
            List<int> move = basic_moves[i];
            make_move(move);
            if (player_turn == player_backup) {
                //  Recursive case: Still players turn
                List<List<int>> post_moves = get_all_valid_moves();
                if (post_moves.Count == 0) {
                    //  Stop case: No more moves
                    output.Add(move);
                }
                else {
                    post_moves = expand_moves(post_moves);
                    for (int p = 0; p < post_moves.Count; p++) {
                        List<int> move_copy = new List<int>(move);
                        move_copy.AddRange(post_moves[p]);
                        output.Add(move_copy);
                    }
                }
            }
            else {
                //  Stop case: No longer our turn
                output.Add(move);
            }
            board = new List<int>(board_backup);
            player_turn = player_backup;
            turns_until_timeout = turn_backup;
            forced_move = forced_backup;
        }
        //  Return
        return output;
    }

    //  Returns all of the valid moves for a given position
    private List<List<int>> get_all_valid_moves_for_pos(int x, int y) {
        if (!is_cords_valid(x, y)) { return null; }
        List<List<int>> output = new List<List<int>>();

        //  Moving Y+ (Black -> White)
        if ((is_black(x, y) || is_king(x, y)) && is_player(x, y)) {
            for (int xm = -1; xm < 2; xm += 2) {
                if (is_empty(x + xm, y + 1)) {
                    List<int> move = new List<int>();
                    move.Add(x); move.Add(y);
                    move.Add(x + xm); move.Add(y + 1);
                    output.Add(move);
                }
                else if (is_opponent(x + xm, y + 1) && is_empty(x + 2 * xm, y + 2)) {
                    List<int> move = new List<int>();
                    move.Add(x); move.Add(y);
                    move.Add(x + 2 * xm); move.Add(y + 2);
                    output.Add(move);
                }
            }
        }
        //  Moving Y- (White -> Black
        if ((is_white(x, y) || is_king(x, y)) && is_player(x, y)) {
            for (int xm = -1; xm < 2; xm += 2) {
                if (is_empty(x + xm, y - 1)) {
                    List<int> move = new List<int>();
                    move.Add(x); move.Add(y);
                    move.Add(x + xm); move.Add(y - 1);
                    output.Add(move);
                }
                else if (is_opponent(x + xm, y - 1) && is_empty(x + 2 * xm, y - 2)) {
                    List<int> move = new List<int>();
                    move.Add(x); move.Add(y);
                    move.Add(x + 2 * xm); move.Add(y - 2);
                    output.Add(move);
                }
            }
        }

        return output;
    }

    //  Return if the designated checker is white
    public bool is_white(int x, int y) {
        if (!is_cords_valid(x, y)) { return false; }
        return (board[cords_to_index(x, y)] % 2 == 0);
    }

    //  Return if the designated checker is black
    public bool is_black(int x, int y) {
        if (!is_cords_valid(x, y)) { return false; }
        return (board[cords_to_index(x, y)] % 2 == 1);
    }

    //  Return if the designated checker belongs to the current player
    public bool is_player(int x, int y) {
        if (!is_cords_valid(x, y)) { return false; }
        return (board[cords_to_index(x, y)] % 2 == player_turn);
    }

    //  Return if the designated checker belongs to the opponent
    public bool is_opponent(int x, int y) {
        if (!is_cords_valid(x, y)) { return false; }
        if (board[cords_to_index(x, y)] == -1) { return false; }
        return (board[cords_to_index(x, y)] % 2 != player_turn);
    }

    //  Return if the designated checker is a king
    public bool is_king(int x, int y) {
        if (!is_cords_valid(x, y)) { return false; }
        return (board[cords_to_index(x, y)] > 1);
    }

    //  Return if the designated square is empty
    public bool is_empty(int x, int y) {
        if (!is_cords_valid(x, y)) { return false; }
        return (board[cords_to_index(x, y)] == CHECKER_NONE);
    }

    //  Return if the cordinates are valid
    public bool is_cords_valid(int x, int y) {
        return ((x >= 0) && (x < 8) && (y >= 0) && (y < 8) && ((x + y) % 2 == 0));
    }

    //  Return if the move is a jump move
    public bool is_jump(List<int> move) {
        return (Mathf.Abs(move[0] - move[2]) == 2);
    }

    //  Return if the ith move is a jump move
    public bool is_jump(List<int> move, int i) {
        return (Mathf.Abs(move[i] - move[i + 2]) == 2);
    }

    //  Convert cordinates to a linear index
    public int cords_to_index(int x, int y) {
        return (x / 2) + 4 * y;
    }

    //  Switch the current player
    public void switch_player() {
        player_turn = (player_turn == 0) ? 1 : 0;
        forced_move.Set(-1, -1);
    }

    //  Find the winner
    public int get_winner() {
        if (turns_until_timeout <= 0) { return PLAYER_DRAW; }
        bool has_white = false, has_black = false;
        for (int p = 0; p < 32; p++) {
            if (board[p] % 2 == PLAYER_WHITE) { has_white = true; }
            if (board[p] % 2 == PLAYER_BLACK) { has_black = true; }
            if (has_white && has_black) { return PLAYER_NONE; }
        }

        if (has_white) { return PLAYER_WHITE; }
        if (has_black) { return PLAYER_BLACK; }
        return PLAYER_NONE;
    }

    //  Use the AI to get a score for the current state of the board
    private float score_board(int player) {
        int winner = get_winner();
        if (winner == PLAYER_NONE) {
            return digital_players[player].GetBoardTotalValue(GetOneHotArray(player));
        }   else {
            return (winner == player ? 1000000 : -1000000);
        }
    }

    //  Get a onehot array for the current board state
    public List<float> GetOneHotArray(int player) {
        List<float> output = new List<float>();
        for (int p = 0; p < 32; p++) {
            if (board[p] == CHECKER_NONE) {
                output.Add(0);
                output.Add(0);
                output.Add(0);
            }   else {
                output.Add(1);
                if (board[p] % 2 == player) { output.Add(1); }
                else { output.Add(0); }
                if (board[p] > 2) { output.Add(1); }
                else { output.Add(0); }
            }
        }
        if (player == PLAYER_BLACK) {
            //  REverse the output in groups of 3
            List<float> rev_out = new List<float>();
            for (int i = output.Count - 3; i >= 0; i -= 3) {
                rev_out.Add(output[i]);
                rev_out.Add(output[i + 1]);
                rev_out.Add(output[i + 2]);
            }
            return rev_out;
        }
        return output;
    }

    //  Set the tile on the board
    private void set_board_tile(int x, int y, int new_checker) {
        if (is_cords_valid(x, y)) {
            int p = cords_to_index(x, y);
            board[p] = new_checker;
        }
        else {
            throw new Exception("Invalid cords (" + x + ", " + y + ")");
        }
    }

    //  Return the state of the given tile
    public int get_board_tile(int x, int y) {
        if (!is_cords_valid(x, y)) {
            return CHECKER_NONE;
        }
        int p = cords_to_index(x, y);
        return board[p];
    }

    //  Get the percentage of moves remaining between two points
    public float GetMovePercentage(int low, int high) {
        float p = ((float)(total_moves - low)) / ((float)(high - low));
        p = 1f - Mathf.Clamp(p, 0, 1);
        return p;
    }

    //  Return the current player
    public int GetPlayerTurn() {
        return player_turn;
    }

    //  A custom class that stores a list of moves and a score for those moves
    public class ValueMovePair {
        public float value;
        public List<int> move;
        public ValueMovePair(float v, List<int> m) {
            value = v;
            move = (m == null) ? new List<int>() : new List<int>(m);
        }
    }
}