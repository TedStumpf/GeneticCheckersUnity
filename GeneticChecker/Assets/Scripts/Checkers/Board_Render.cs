using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board_Render : MonoBehaviour {
    public Transform tile;
    private Transform[][] tiles;

	// Use this for initialization
	void Awake () {
        //  Create tile array
        tiles = new Transform[8][];
        for (int x = 0; x < 8; x++) {
            tiles[x] = new Transform[8];
            for (int y = 0; y < 8; y++) {
                tiles[x][y] = Instantiate(tile, this.transform);
                tiles[x][y].transform.position = new Vector3(20 * x, 20 * y, 0);
                Board_Tile_Contol btc = tiles[x][y].GetComponent<Board_Tile_Contol>();
                btc.tile_is_black = ((x + y) % 2 == 1);
                btc.x = x;
                btc.y = y;
                btc.Update_Render();
            }
        }
	}

    //  Resize to fit the screen
    public void Resize(RectTransform rect_trans) {
        //  Get bounds
        Rect rect = rect_trans.rect;
        Vector3 min = new Vector3(rect.xMin, rect.yMin, -2);
        Vector3 max = new Vector3(rect.xMax, rect.yMax, -2);

        //  Reposition
        transform.localPosition = new Vector3();

        //  Convert to worldspace cords
        min = Camera.main.ViewportToWorldPoint(min);
        max = Camera.main.ViewportToWorldPoint(max);

        //  Scale
        float width = max.x - min.x;
        float height = max.y - min.y;

        float w_scale = width / 8;
        float h_scale = height / 8;

        Vector3 scale = transform.localScale;
        scale.x = w_scale;
        scale.y = h_scale;
        transform.localScale = scale;
    }

    //  Update an individual tile
    public void Update_Tile(int x, int y, int checker, float priority, bool king, int valid_state) {
        Board_Tile_Contol btc;
        btc = tiles[x][y].GetComponent<Board_Tile_Contol>();
        if (btc == null) {
            Debug.Log("Error on BTC for " + x + ", " + y);
        }
        btc.checker_color = checker;
        btc.tile_priority = priority;
        btc.is_king = king;
        btc.valid_state = valid_state;
        btc.Update_Render();
    }
}
