using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaveFunctionCollapse : MonoBehaviour
{
    [SerializeField] Tilemap InputMap; //The input tilemap to take
    [SerializeField] Tilemap outputMap; //Where the output will be going
    TileBase[] allTiles; //This is needed to take a 1D array of tiles form the inputTilemap. 
    BoundsInt bounds; //The area of the tilemap. needed to lopop through the 1D array, allTiles, to put into our 2D array. 
    TileBase[,] inputTiles; //A 2D array that we create to take the info from each of our tiles before transfering it to your 2D araay of containers. A later update could likely remove this. 
    TileContainer[,] inputMapContainer; //Our 2D array that we will be using to go through each tile, adding neighbours, before recombining into a list. 
    List<TileContainer> AllPossibleTiles = new List<TileContainer>(); //Our list that we will be using for WFC itself. this will have the info of each tile, and what each possible neighbour can be. 

    List<TileContainer>[,] outputArray; //This array of lists holds the output tiles we will be using. 
    public int outputArraySize; //How big we want to make our output array size. 
    Vector2Int entropyHandlerVector; //When we calculate entropy, we are going to find any element that has not been solved , but is less then any element in the list. 
    bool foundEntropy = false; //This marks when we have found places that have less then the normal amount of options
    public bool wrapping = true; //This handles wrapping elements from the array. if they are wrapping, then edge elements will select items from the other side of the array. 
                                 //Turning wrapping off decreases solve rate. 

    //will need an editor button to run the script
    // Start is called before the first frame update

    public void Run()
    {
        WFCHumbleObject wfcHelper = new WFCHumbleObject(InputMap, outputMap, outputArraySize, wrapping);
        wfcHelper.RunWFC();

    }
}

public class WFCHumbleObject
{
    [SerializeField] Tilemap InputMap; //The input tilemap to take
    [SerializeField] Tilemap outputMap; //Where the output will be going
    TileBase[] allTiles; //This is needed to take a 1D array of tiles form the inputTilemap. 
    BoundsInt bounds; //The area of the tilemap. needed to lopop through the 1D array, allTiles, to put into our 2D array. 
    TileBase[,] inputTiles; //A 2D array that we create to take the info from each of our tiles before transfering it to your 2D araay of containers. A later update could likely remove this. 
    TileContainer[,] inputMapContainer; //Our 2D array that we will be using to go through each tile, adding neighbours, before recombining into a list. 
    List<TileContainer> AllPossibleTiles = new List<TileContainer>(); //Our list that we will be using for WFC itself. this will have the info of each tile, and what each possible neighbour can be. 

    List<TileContainer>[,] outputArray; //This array of lists holds the output tiles we will be using. 
    public int outputArraySize; //How big we want to make our output array size. 
    Vector2Int entropyHandlerVector; //When we calculate entropy, we are going to find any element that has not been solved , but is less then any element in the list. 
    bool foundEntropy = false; //This marks when we have found places that have less then the normal amount of options
    public bool wrapping = true; //This handles wrapping elements from the array. if they are wrapping, then edge elements will select items from the other side of the array. 
                                 //Turning wrapping off decreases solve rate. 
    private bool printToConsole;
    //will need an editor button to run the script
    // Start is called before the first frame update

    public WFCHumbleObject(Tilemap InputMap, Tilemap outputMap, int outputArraySize, bool wrapping, bool printToConsole = false)
    {
        this.InputMap = InputMap;
        this.outputMap = outputMap;
        this.outputArraySize = outputArraySize;
        this.wrapping = wrapping;
        this.printToConsole = printToConsole;
    }
    public bool RunWFC()
    {
        InputTileHandler(); //Handles the input we are working with, converting it from a tilemap, into a list of possibletiles.
        WFCSetup(); //takes the list of possible tiles and converts them into an array full of possibilities. 

        //A simple do while loop handles us passing through each object one at a time. 
        //If checkifComplete returns true, we keep going through until we finally finish each element in the array. 
        int maxIterations = 10000;
        bool dountilcomplete = false;
        do
        {

            // We check each time we loop whether we have elements that have less then the starting number of elements in each section. 
            //If we do, we use those instead of a random point, as that point is closer to being collapsed, and causes less issues. 


            if (foundEntropy)
            {
                WFCwithEntropy();
                foundEntropy = false;
            }
            else
            {
                WFCRandomPass();
            }
            maxIterations--;
            dountilcomplete = checkifComplete();
        } while (dountilcomplete == false && maxIterations > 0);
        if (dountilcomplete == false || maxIterations <= 0)
        {
            return false;
        }

        outputHandler();
        return true;
    }

    private void WFCRandomPass()
    {
        //This process is called for the first loop, and if there are no squares that are less then the max count. 
        int xRandom = UnityEngine.Random.Range(0, outputArraySize);
        int yRandom = UnityEngine.Random.Range(0, outputArraySize);

        //we only need to check the first element, as if there is more then one element, it most definetly has not been collapsed. 
        if (outputArray[xRandom, yRandom].First<TileContainer>().hasBeenCollapsed == false)
        {
            Collapse(xRandom, yRandom);
            WaveFunction(xRandom, yRandom);
            CollisionCheck();

        }
        else
        {
            PrintToConsole("Has already Been collapsed at " + xRandom + " " + yRandom);
        }
    }

    private void WFCwithEntropy()
    {
        //we take in the element that our entropy calculator gave us, and we use that point instead of a random element. 

        if (outputArray[entropyHandlerVector.x, entropyHandlerVector.y].First<TileContainer>().hasBeenCollapsed == false)
        {
            Collapse(entropyHandlerVector.x, entropyHandlerVector.y);
            WaveFunction(entropyHandlerVector.x, entropyHandlerVector.y);
            CollisionCheck();
        }

    }


    private void EntropyHandler()
    {
        //we create a list of all possible lower entropy points, and then we choose a random one from out list. 
        //we also mark that we found entropy, as it allows us to enter the entropy version of wfc, instead of the normal one. 

        List<Vector2Int> tempListofEntropy = new List<Vector2Int>();
        for (int x = 0; x < outputArraySize; x++)
        {
            for (int y = 0; y < outputArraySize; y++)
            {
                if (outputArray[x, y].First<TileContainer>().hasBeenCollapsed == false)
                {
                    if (outputArray[x, y].Count != AllPossibleTiles.Count)
                    {
                        tempListofEntropy.Add(new Vector2Int(x, y));
                        foundEntropy = true;
                        PrintToConsole(" We found some!");
                    }
                }
            }
        }
        if (foundEntropy)
        {
            //if we found entropy, we need to select a random element from our list, and then use that value. 

            int RandomElementfromList = UnityEngine.Random.Range(0, tempListofEntropy.Count());
            entropyHandlerVector = tempListofEntropy[RandomElementfromList];

            //Doing some small clean up here, so we save a small amount on space. We dont delete other elements in case we need to start over for any reason.

            tempListofEntropy.Clear();
        }
    }

    bool checkifComplete()
    {
        //This checks to see if every tile has been collapsed/
        //If every tile has not been collapsed, then we need to continue running the algorhthm.
        //If it has, then we can output the array.

        for (int x = 0; x < outputArraySize; x++)
        {
            for (int y = 0; y < outputArraySize; y++)
            {
                if (outputArray[x, y][0].hasBeenCollapsed == false)
                {
                    return false;
                }
            }
        }

        PrintToConsole("All done!");
        return true;
    }

    public void CollisionCheck()
    {
        //This checks if we have collidedm and no longer have any available options for a square. if we dont, then we cannot finish the output. 
        // We can implement a mariad of things to do here. We could implement a Clear function, that resets the various elements around this one and see that were left with. 
        //In this instance, Ive simple asked we restart the program and do it again. 

        for (int x = 0; x < outputArraySize; x++)
        {
            for (int y = 0; y < outputArraySize; y++)
            {
                if (outputArray[x, y].Count == 0)
                {
                    //We have a collision
                    throw new Exception("We have a collision, PLease restart. COllision at " + x + " and " + y);


                }
            }
        }
    }

    /// <summary>
    /// This was an idea I had to restart from the beginging, however for some reason this causes unity to hang, and a memory leak to develop. It will take some more looking into to see what is going wrong. 
    /// </summary>
    private void Reset()
    {
        for (int x = 0; x < outputArraySize; x++)
        {
            for (int y = 0; y < outputArraySize; y++)
            {
                outputArray[x, y].Clear();
                foundEntropy = false;
            }
        }
        WFCSetup();
    }
    void Collapse(int xPosition, int yPosition)
    {
        //This is where we process the array. We choose a random element of the list, and delete all the others. This is now our collapsed position. 
        // We then set that it has been collapsed, and remove all the other elements.


        int RandomChoice = UnityEngine.Random.Range(0, outputArray[xPosition, yPosition].Count);
        outputArray[xPosition, yPosition][RandomChoice].hasBeenCollapsed = true;
        outputArray[xPosition, yPosition].RemoveAll(i => i.hasBeenCollapsed == false);

        /*    PrintToConsole("Collapsed" + xPosition + " and " + yPosition);
            foreach(TileContainer leftover in outputArray[xPosition,yPosition])
            {
                PrintToConsole( "Leftover tiles (should be one) "+leftover.SingleTile.name);
            }*/

        //We are left with a tile simpletile, and a list of tiles in each direction.

        //The next thing to do is the wave function. This removes the elements from the four cardinal direction squares attached to this one, supposing there is one, and lets them know they can only contain this squares info. 

    }

    void WaveFunction(int xPosition, int yPosition)
    {
        //Checks to see if there is an element in the array in this position. if not it doesnt do anything.
        //It also doesnt do anything if the element in the position has already been collapsed. 
        //If there is it, it checks to see if any element in the list is not in the list of possible tiles left over. 
        //If there are, they need to be removed, as there is no longer a chance that they can be possible tiles. 
        //They are marked to be removed, and then are removed from the list.
        //This happens for each of the 4 tiles adjacent to the one we have collapsed. 

        if (xPosition + 1 < outputArraySize)
        {
            //This loop handles the one in the up direction
            if (outputArray[xPosition + 1, yPosition].First<TileContainer>().hasBeenCollapsed == false)
            {
                foreach (TileContainer tileToSeeifInList in outputArray[xPosition + 1, yPosition])
                {
                    if (!outputArray[xPosition, yPosition][0].possibleUpTiles.Contains(tileToSeeifInList.SingleTile))
                    {
                        tileToSeeifInList.removethisElement = true;
                        PrintToConsole("up Removing" + tileToSeeifInList.SingleTile.name);
                    }
                }
            }
            outputArray[xPosition + 1, yPosition].RemoveAll(i => i.removethisElement == true);

        }
        else
        {
            PrintToConsole("NO up element");
        }

        if (xPosition - 1 >= 0)
        {
            //This loop handles the ones in the down direction

            if (outputArray[xPosition - 1, yPosition].First<TileContainer>().hasBeenCollapsed == false)
            {
                foreach (TileContainer tileToSeeifInList in outputArray[xPosition - 1, yPosition])
                {
                    if (!outputArray[xPosition, yPosition][0].possibleDownTiles.Contains(tileToSeeifInList.SingleTile))
                    {
                        tileToSeeifInList.removethisElement = true;
                        PrintToConsole("down Removing" + tileToSeeifInList.SingleTile.name);
                    }
                    else
                    {
                        PrintToConsole("Didnt remove this element, is in our list down");
                    }
                }
            }

            outputArray[xPosition - 1, yPosition].RemoveAll(i => i.removethisElement == true);
        }
        else
        {
            PrintToConsole("NO down element");
        }

        if (yPosition + 1 < outputArraySize)
        {
            //This loop handles the one in the left direction
            if (outputArray[xPosition, yPosition + 1].First<TileContainer>().hasBeenCollapsed == false)
            {
                foreach (TileContainer tileToSeeifInList in outputArray[xPosition, yPosition + 1])
                {
                    if (!outputArray[xPosition, yPosition][0].possibleLeftTiles.Contains(tileToSeeifInList.SingleTile))
                    {
                        tileToSeeifInList.removethisElement = true;
                        PrintToConsole("left Removing" + tileToSeeifInList.SingleTile.name);
                    }
                    else
                    {
                        PrintToConsole("Didnt remove this element, is in our list left");
                    }
                }
            }

            outputArray[xPosition, yPosition + 1].RemoveAll(i => i.removethisElement == true);
        }
        else
        {
            PrintToConsole("NO left element");
        }

        if (yPosition - 1 >= 0)
        {
            //This loop handles the ones in the right direction
            if (outputArray[xPosition, yPosition - 1].First<TileContainer>().hasBeenCollapsed == false)
            {
                foreach (TileContainer tileToSeeifInList in outputArray[xPosition, yPosition - 1])
                {
                    if (!outputArray[xPosition, yPosition][0].possibleRightTiles.Contains(tileToSeeifInList.SingleTile))
                    {
                        tileToSeeifInList.removethisElement = true;
                        PrintToConsole("right Removing" + tileToSeeifInList.SingleTile.name);
                    }
                    else
                    {
                        PrintToConsole("Didnt remove this element, is in our list right");
                    }
                }
            }

            outputArray[xPosition, yPosition - 1].RemoveAll(i => i.removethisElement == true);
        }
        else
        {
            PrintToConsole("NO right element");
        }
    }


    void WFCSetup()
    {

        //Start by creating an array of the correct sizing for the given output. 
        outputArray = new List<TileContainer>[outputArraySize, outputArraySize];

        //Then loop through it, and put each possible tile in each spot. This is the unconstrainted step
        for (int x = 0; x < outputArraySize; x++)
        {
            for (int y = 0; y < outputArraySize; y++)
            {
                //  TileContainer[] tempArray = new TileContainer[AllPossibleTiles.Count];
                //  AllPossibleTiles.CopyTo(tempArray);
                //  outputArray[x, y] = new List<TileContainer>(tempArray.ToList<TileContainer>());
                outputArray[x, y] = new List<TileContainer>();
                foreach (TileContainer ToCopy in AllPossibleTiles)
                {

                    outputArray[x, y].Add(ToCopy.DeepCopy());
                }
                // outputArray[x, y].AddRange(AllPossibleTiles);

                //Now we go thorugh each option in the list and make the collisionx and y, the location of the square in case we need to find it, due to a collisison.
                foreach (TileContainer colhandler in outputArray[x, y])
                {
                    colhandler.collisionX = x;
                    colhandler.collisionY = y;
                }
            }
        }

    }



    void InputTileHandler()
    {
        InputMap.CompressBounds(); //Sets the array to the area of the tiles, in case something went wrong. 
        bounds = InputMap.cellBounds;
        TileBase[] allTiles = InputMap.GetTilesBlock(bounds); //the bounds above is used to get the tiles in this area. 
        inputTiles = new TileBase[bounds.size.x, bounds.size.y]; // creates our array of tiles in the right size.
        inputMapContainer = new TileContainer[bounds.size.x, bounds.size.y]; //creating the size of the array so we can start intrializing the lists, and putting the info in

        // nested for loop. We get a 1D array of elements when we call get the tiles, and to simplify the information we should have a 2d array. 
        for (int x = 0; x < bounds.size.x; x++)
            for (int y = 0; y < bounds.size.y; y++)
            {
                if (allTiles[x + y * bounds.size.x] == null)
                {
                    //The array should have no holes in it, but just in case. 

                    PrintToConsole("Null tile, will not work, location: " + x + " and " + y);
                    break;
                }
                //This take the 2D array, and turns it into a 2D array. 

                inputTiles[x, y] = allTiles[x + y * bounds.size.x];

                //All of these need to be initialized now, or they return errors later on. This is the easiest place for the input array. 
                //These are the tile itself, and each of its possible neighbours, plus all the tiles that can be in there, though this isnt used as of now. 
                inputMapContainer[x, y] = new TileContainer(inputTiles[x, y], x, y);
                inputMapContainer[x, y].possibleUpTiles = new List<TileBase>();
                inputMapContainer[x, y].possibleDownTiles = new List<TileBase>();
                inputMapContainer[x, y].possibleLeftTiles = new List<TileBase>();
                inputMapContainer[x, y].possibleRightTiles = new List<TileBase>();
                inputMapContainer[x, y].possibleTilesForThis = new List<TileBase>();
                inputMapContainer[x, y].removethisElement = false;
            }

        for (int x = 0; x < bounds.size.x; x++)
            for (int y = 0; y < bounds.size.y; y++)
            {
                //This loop sets all the possible neighbours for each tile. 
                //In each iteration it checks the neighnbour next to it, to see if it is ourside the bounding box,
                //and if it isnt, it gets that info from the array in that spot. 
                //if it is outsidde the box, it wraps around to its closest neighbour
                //This step is done after the last step is fully finished as, while it may have seemed like it would work in the last one,
                //it simply doesnt due to the first tiles never being able to find what their neighbours have. 

                //In this statement we handle the downwards element from our current position. This also handles wrapping of our element
                //In case we are at an edge, it wraps around to the other side of our array. 

                if (x + 1 < bounds.size.x) //If our current position +1 is outside the bounds of the array, we wrap around to the other side of the array, if wrapping is true. 
                {
                    inputMapContainer[x, y].possibleDownTiles.Add(inputMapContainer[x + 1, y].SingleTile);
                }
                else if (wrapping == true)
                {
                    inputMapContainer[x, y].possibleDownTiles.Add(inputMapContainer[0, y].SingleTile);
                }

                //This statement handles the Up direction. 

                if (x - 1 >= 0)
                {
                    inputMapContainer[x, y].possibleUpTiles.Add(inputMapContainer[x - 1, y].SingleTile);
                }
                else if (wrapping == true)
                {
                    inputMapContainer[x, y].possibleUpTiles.Add(inputMapContainer[bounds.size.x - 1, y].SingleTile);
                }

                //This statement handles the right direction

                if (y - 1 >= 0)
                {
                    inputMapContainer[x, y].possibleRightTiles.Add(inputMapContainer[x, y - 1].SingleTile);
                }
                else if (wrapping == true)
                {
                    inputMapContainer[x, y].possibleRightTiles.Add(inputMapContainer[x, bounds.size.y - 1].SingleTile);
                }

                //This statement handles the Left direction

                if (y + 1 < bounds.size.y)
                {
                    inputMapContainer[x, y].possibleLeftTiles.Add(inputMapContainer[x, y + 1].SingleTile);
                }
                else if (wrapping == true)
                {
                    inputMapContainer[x, y].possibleLeftTiles.Add(inputMapContainer[x, 0].SingleTile);
                }
            }

        //This step now needs to take each element of the array, and combine them into the list.
        //To do this, we will check if a list contains elements from each part of the array, and if they are the same, we will add each neightbour to  a list of each for that, checking again if any neihbours are the same

        foreach (TileContainer TileToCheckfrominput in inputMapContainer)
        {
            if (AllPossibleTiles == null)
            {
                //Double check we dont have a null list. Small piece of mind. 

                PrintToConsole("Not initialized");
                AllPossibleTiles.Add(TileToCheckfrominput);
            }
            else
            {
                TileContainer contains = AllPossibleTiles.Find(ListTiles => ListTiles.SingleTile.name == TileToCheckfrominput.SingleTile.name);

                if (contains != null)
                {
                    PrintToConsole("Concat elements");
                    contains.possibleUpTiles.Union<TileBase>(TileToCheckfrominput.possibleUpTiles);
                }
                else
                {
                    AllPossibleTiles.Add(TileToCheckfrominput);
                }
            }
        }

        // In this step we are trying to combine the elements of the list. If two or more elements have the same tile, they combine their lists of possible directional tiles. 
        //This gives us a shorter list that contains all possiblities for them. 

        for (int TileToCheckListA = AllPossibleTiles.Count - 1; TileToCheckListA >= 0; TileToCheckListA--)
        {
            for (int TileToCheckListB = AllPossibleTiles.Count - 1; TileToCheckListB >= 0; TileToCheckListB--)
            {
                if (AllPossibleTiles[TileToCheckListA].SingleTile.name == AllPossibleTiles[TileToCheckListB].SingleTile.name)
                {
                    if (AllPossibleTiles[TileToCheckListA].collisionX != AllPossibleTiles[TileToCheckListB].collisionX || AllPossibleTiles[TileToCheckListA].collisionY != AllPossibleTiles[TileToCheckListB].collisionY)
                    {
                        if (!AllPossibleTiles[TileToCheckListA].removethisElement)
                        {
                            if (!AllPossibleTiles[TileToCheckListB].removethisElement)
                            {
                                //There are for checks here to keep it clean on what each one is doing. 
                                //The first checks to see if two tiles in our list have the same name, only these are candidates to be combined in this section
                                //The second checks to make sure they arent at the same position, as if both have the exact same position, they are the same tile, and we dont want to combine them.
                                //The last checks to make sure one of them hasnt already been iterated over. We cant remove the tiles while trying to loop through them, as it causes exception errors. 
                                //Instead we mark a bool from the one we are going to remove, and have taken all the information from, and add it to our first one. 
                                //We also call Distinct toList to remove the aditional elements from the list, after we add them together. This leaves us with only 1 set of each possible tile. 

                                AllPossibleTiles[TileToCheckListA].possibleUpTiles.AddRange(AllPossibleTiles[TileToCheckListB].possibleUpTiles);
                                AllPossibleTiles[TileToCheckListA].possibleUpTiles = AllPossibleTiles[TileToCheckListA].possibleUpTiles.Distinct().ToList();



                                AllPossibleTiles[TileToCheckListA].possibleDownTiles.AddRange(AllPossibleTiles[TileToCheckListB].possibleDownTiles);
                                AllPossibleTiles[TileToCheckListA].possibleDownTiles = AllPossibleTiles[TileToCheckListA].possibleDownTiles.Distinct().ToList();


                                AllPossibleTiles[TileToCheckListA].possibleLeftTiles.AddRange(AllPossibleTiles[TileToCheckListB].possibleLeftTiles);
                                AllPossibleTiles[TileToCheckListA].possibleLeftTiles = AllPossibleTiles[TileToCheckListA].possibleLeftTiles.Distinct().ToList();



                                AllPossibleTiles[TileToCheckListA].possibleRightTiles.AddRange(AllPossibleTiles[TileToCheckListB].possibleRightTiles);
                                AllPossibleTiles[TileToCheckListA].possibleRightTiles = AllPossibleTiles[TileToCheckListA].possibleRightTiles.Distinct().ToList();


                                AllPossibleTiles[TileToCheckListB].removethisElement = true;
                            }
                        }
                    }
                }
            }
        }

        //This removes all the extra tiles we have, otherwise we would have a very large list of tiles we dont want. 

        AllPossibleTiles.RemoveAll(i => i.removethisElement == true);

        foreach (TileContainer match in AllPossibleTiles)
        {
            string mainname = "Main: " + match.SingleTile.name, upnames = "\n UP ", downnames = "\n Down ", leftnames = " \n Left ", rightnames = "\n Right ";

            foreach (TileBase match2 in match.possibleUpTiles)
            {
                upnames = upnames + " \n" + match2.name;
            }

            foreach (TileBase match3 in match.possibleDownTiles)
            {
                downnames = downnames + " \n" + match3.name;
            }

            foreach (TileBase match4 in match.possibleLeftTiles)
            {
                leftnames = leftnames + " \n" + match4.name;
            }

            foreach (TileBase match5 in match.possibleRightTiles)
            {
                rightnames = rightnames + " \n" + match5.name;
            }

            PrintToConsole(mainname + upnames + downnames + leftnames + rightnames);
        }
    }

    //When we are finished this outputs the elements we have chosen to our output Tilemap.
    //We only do first, as there should only be one option to go there anyways. 

    void outputHandler()
    {
        for (int x = 0; x < outputArraySize; x++)
        {
            for (int y = 0; y < outputArraySize; y++)
            {
                outputMap.SetTile(new Vector3Int(x, y, 0), outputArray[x, y].First<TileContainer>().SingleTile);
            }
        }
    }

    private void PrintToConsole(string message)
    {
        if (printToConsole)
        {
            Debug.Log(message);
        }
    }
}
