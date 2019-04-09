using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board_Tile_Contol : MonoBehaviour {
    public bool is_king, tile_is_black;
    public int checker_color = -1, x = -1, y = -1, valid_state = 0;
    public float tile_priority = 0;
    public Transform child;
    public Sprite checker, king_checker;
    public Active_Board control_parent;

	// Use this for initialization
	void Awake () {
        Update_Render();
    }

    //  Passes the click event upwards to the grandparent object
    public void OnClick(GameObject trigger) {
        transform.parent.parent.GetComponent<Active_Board>().PlayerClick(trigger);
    }

    //  Update the display
    public void Update_Render() {
        UnityEngine.UI.Image sr = GetComponent<UnityEngine.UI.Image>();

        //  Change tile color
        if (tile_is_black) {
            sr.color = new Color(0.1f, 0.1f, 0.1f);
        }   else {
            float inv = 1 - tile_priority;
            sr.color = new Color(1, inv, inv);
        }

        //  Highlight for user state
        if (valid_state == 1) {
            sr.color = new Color(0.8f, 0.8f, 0.5f);
        } else if (valid_state == 2) {
            sr.color = new Color(0.8f, 0.8f, 0);
        }

        //  Update displayed checker
        UnityEngine.UI.Image csr = child.GetComponent<UnityEngine.UI.Image>();
        if (checker_color == -1) {
            csr.enabled = false;
        }   else {
            csr.enabled = true;
            if (is_king) { csr.sprite = king_checker; }
            else { csr.sprite = checker; }

            if (checker_color == 0) {
                csr.color = new Color(1, 1, 1);
            }   else {
                csr.color = new Color(0.1f, 0.1f, 0.1f);
            }
        }
    }
}
