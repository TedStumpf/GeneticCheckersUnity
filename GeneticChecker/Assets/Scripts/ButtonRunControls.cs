using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonRunControls : MonoBehaviour {
    
    //  This module provides funtionality to some of the buttons

	public void RunOnce(Overseer overseer_game_object) {
        overseer_game_object.RunOnce();
    }

    public void RunAll(Overseer overseer_game_object) {
        overseer_game_object.RunAll();
    }
}
