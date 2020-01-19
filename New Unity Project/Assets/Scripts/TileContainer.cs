using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Tilemaps;

public class TileContainer: MonoBehaviour
{

 public TileBase SingleTile; //The tile that is represented in each container. 

    //The following tiles all represent tiles in possible directions from the one we are looking at. 

 public List<TileBase> possibleUpTiles; 
 public  List<TileBase> possibleDownTiles;
 public  List<TileBase> possibleLeftTiles;
 public  List<TileBase> possibleRightTiles;
 public List<TileBase> possibleTilesForThis;

    //This is used to hold the location of the tile we are at in our output array. ALso used to find the collision, if there is one. 

    public int collisionX;
    public int collisionY;

    //We use this element to delete containers that we have already taken info from when we combine them. 

    public bool removethisElement = false;

    //This is set to true once we have decided on a single possible tile for the algorythm. It returns true if we try and do it again. 

    public  bool hasBeenCollapsed = false;

    //This is set if we have an error where we dont have anymore tiles we can use. 
 public bool hasCollision = false;


    public TileContainer(TileBase newTile, int xPos, int Ypos)
    {
        SingleTile = newTile;
        collisionX = xPos;
        collisionY = Ypos;
    }

    public TileContainer()
    {
        // This is needed for the output tiles so I can create them with no issues.
    }

    //This is a Deep copy. If this isnt added into here, we have issues making single copies of all the elements for the whole array when trying to go over it. 
    //This lets us make a copy that contains all the information we need, however isnt a reference to the original one, otherwise any chance we made to one copy in our array would change them all. 

    public TileContainer DeepCopy()
    {
        TileContainer Copy = new TileContainer();
        Copy.SingleTile = this.SingleTile;
        Copy.possibleUpTiles = this.possibleUpTiles;
        Copy.possibleDownTiles = this.possibleDownTiles;
        Copy.possibleLeftTiles = this.possibleLeftTiles;
        Copy.possibleRightTiles = this.possibleRightTiles;
        Copy.possibleTilesForThis = this.possibleTilesForThis;
        Copy.collisionX = this.collisionX;
        Copy.collisionY = this.collisionY;
        Copy.removethisElement = this.removethisElement;
        Copy.hasBeenCollapsed = this.hasBeenCollapsed;
        Copy.hasCollision = this.hasCollision;

        return Copy;
    }
}
