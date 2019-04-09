using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OverseerPanelManager : MonoBehaviour {
    public GameObject status_object, task_object, score_object,
        button_single, button_multi, button_repeat,
        board_render;
    
    //  Updates the status text
    public void DisplayUpdateStatus(string text) {
        Text text_comp = status_object.GetComponent<Text>();
        text_comp.text = text;
    }

    //  Updates the tasks text
    public void DisplayUpdateTasks(string text) {
        Text text_comp = task_object.GetComponent<Text>();
        text_comp.text = text;
    }

    //  Updates the display score
    public void DisplayUpdateScore(string text) {
        Text text_comp = score_object.GetComponent<Text>();
        text_comp.text = text;
    }

    //  Set the buttons status to Run
    public void SetButtonTextRun() {
        Text text_comp_s = button_single.GetComponentInChildren<Text>();
        text_comp_s.text = "Run Once";
        button_multi.active = true;
        Text text_comp_m = button_multi.GetComponentInChildren<Text>();
        text_comp_m.text = "Run All";
    }

    //  Set the buttons status to Sort
    public void SetButtonTextSort() {
        Text text_comp_s = button_single.GetComponentInChildren<Text>();
        text_comp_s.text = "Sort";
        button_multi.active = false;
    }

    //  Set the buttons status to Next
    public void SetButtonTextNext() {
        Text text_comp_s = button_single.GetComponentInChildren<Text>();
        text_comp_s.text = "Next Generation";
        button_multi.active = false;
    }
}
