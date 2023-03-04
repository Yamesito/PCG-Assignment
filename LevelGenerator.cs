using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;


public enum TileType {
    Empty = 0,
    Player,
    Enemy,
    Wall,
    Door,
    Key,
    Dagger,
    End
}

public class LevelGenerator : MonoBehaviour
{
    public GameObject[] tiles;
    protected void Start()
    {        
        int width = 64;
        int height = 64;
        float scale = 15f;
        float sensitivity = 0.07f;
        TileType[,] grid = new TileType[height, width];
        int[,] map = PerlinMap(height, width, scale, sensitivity);
        FillMapWall(map, grid);
        Dictionary<int, HashSet<int[]>> rooms = SearchRooms(map);
        Dictionary<int, int[]> roomCentroids = GetRoomCentroids(rooms);
        CreateBoxRooms(roomCentroids, grid);
        HashSet<int[]> hallways = FindHallways(roomCentroids, (width+height)/2);
        CreateHallways(hallways, grid);
        GenerateMapBorder(map, grid);
        LocateIndividualObjects(grid, roomCentroids);

        //TODO
        //FillBlock(grid, 26, 26, 12, 12, TileType.Empty);
        

        //Debugger.instance.AddLabel(32, 26, "Room 1");

        //use 2d array (i.e. for using cellular automata)
        CreateTilesFromArray(grid);
    }

    //fill part of array with tiles
    private void FillBlock(TileType[,] grid, int x, int y, int width, int height, TileType fillType) {
        for (int tileY=0; tileY<height; tileY++) {
            for (int tileX=0; tileX<width; tileX++) {
                grid[tileY + y, tileX + x] = fillType;
            }
        }
    }

    //use array to create tiles
    private void CreateTilesFromArray(TileType[,] grid) {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        for (int y=0; y<height; y++) {
            for (int x=0; x<width; x++) {
                 TileType tile = grid[y, x];
                 if (tile != TileType.Empty) {
                     CreateTile(x, y, tile);
                 }
            }
        }
    }

    //create a single tile
    private GameObject CreateTile(int x, int y, TileType type) {
        int tileID = ((int)type) - 1;
        if (tileID >= 0 && tileID < tiles.Length)
        {
            GameObject tilePrefab = tiles[tileID];
            if (tilePrefab != null) {
                GameObject newTile = GameObject.Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                newTile.transform.SetParent(transform);
                return newTile;
            }

        } else {
            Debug.LogError("Invalid tile type selected");
        }
        return null;
    }
    //Creates a 2D array using perlin noise and descretizes it
    private int[,] PerlinMap(int height, int width, float scale, float sensitivity){
        int[,] map = new int[height, width];
        float sample;
        float perlinX;
        float perlinY;
        Random.seed = System.DateTime.Now.Millisecond;
        float offsetX = Random.Range(0f, 9999f);
        float offsetY = Random.Range(0f, 9999f);
        for (int i = 0; i < height; i++){
            for (int j = 0; j < width; j++){
                perlinX = (float)j/width * scale + offsetX;
                perlinY = (float)i/height * scale + offsetY;
                sample = Mathf.PerlinNoise(perlinX, perlinY);
                if (sample > sensitivity){
                    map[i, j] = 1;
                }
                else{
                    map[i, j] = 0;
                }
            }
        }
        return map;
    }
    
    public void FillMapWall(int[,] map, TileType[,] grid){
        int height = map.GetLength(0);
        int width = map.GetLength(1);
        for (int i = 0; i < height; i++){
            for (int j = 0; j < width; j++){
                if (map[i,j] == 1){
                    grid[i,j] = TileType.Wall;
                }
            }
        }
    }
    
    public int GetRoomColor(Dictionary<int, HashSet<int[]>> rooms, int x, int y){
        foreach (KeyValuePair<int, HashSet<int[]>> room in rooms){
            foreach (int[] tile in room.Value){
                if (tile[0] == x && tile[1] == y){
                    return room.Key;
                }
            }
        }
        return -1;
    }

    public bool IsEmpty(int[,] map, int x, int y){
        if (map[x,y] == 0){
            return true;
        }
        return false;
    }

    public void RecursiveRoomChecker(int[,] map, Dictionary<int, HashSet<int[]>> rooms, int x, int y){
        //choose room color
        int color;
        if (x > 1 && y > 1 && x < map.GetLength(0)-1 && y < map.GetLength(1)-1){
            if (GetRoomColor(rooms, x, y-1) != -1){
                color = GetRoomColor(rooms, x, y-1);
                rooms[color].Add(new int[2]{x,y});
            } else if (GetRoomColor(rooms, x-1, y) != -1){
                color = GetRoomColor(rooms, x-1, y);
                rooms[color].Add(new int[2]{x,y});
            } else if (GetRoomColor(rooms, x+1, y) != -1){
                color = GetRoomColor(rooms, x+1, y);
                rooms[color].Add(new int[2]{x,y});
            } else if (GetRoomColor(rooms, x, y+1) != -1){
                color = GetRoomColor(rooms, x, y+1);
                rooms[color].Add(new int[2]{x,y});
            } else{
                color = rooms.Count;
                rooms[color] = new HashSet<int[]>();
                rooms[color].Add(new int[2]{x,y});
            }
            //recursive calls
            if (IsEmpty(map, x+1, y)){
                if (GetRoomColor(rooms, x+1, y) == -1){
                    rooms[color].Add(new int[2]{x+1,y});
                    RecursiveRoomChecker(map, rooms, x+1, y);
                }
            }
            if (IsEmpty(map, x, y+1)){
                if (GetRoomColor(rooms, x, y+1) == -1){
                    rooms[color].Add(new int[2]{x,y+1});
                    RecursiveRoomChecker(map, rooms, x, y+1);
                }
            }
            if (IsEmpty(map, x-1, y)){
                if (GetRoomColor(rooms, x-1, y) == -1){
                    rooms[color].Add(new int[2]{x-1,y});
                    RecursiveRoomChecker(map, rooms, x-1, y);
                }
            }
            if (IsEmpty(map, x, y-1)){
                if (GetRoomColor(rooms, x, y-1) == -1){
                    rooms[color].Add(new int[2]{x,y-1});
                    RecursiveRoomChecker(map, rooms, x, y-1);
                }
            }
        }
    }
    public Dictionary<int, HashSet<int[]>> SearchRooms(int[,] map){
        int height = map.GetLength(0);
        int width = map.GetLength(1);
        Dictionary<int, HashSet<int[]>> rooms = new Dictionary<int, HashSet<int[]>>();
        for (int i = 0; i < height; i++){
            for (int j = 0; j < width; j++){
                if (IsEmpty(map, i, j) && GetRoomColor(rooms, i, j) == -1){//if it is an empty space and doesnt have an assigned color
                    RecursiveRoomChecker(map, rooms, i, j);
                }
            }
        }
        return rooms;
    }

    public void GenerateMapBorder(int[,] map, TileType[,] grid){
        int height = map.GetLength(0);
        int width = map.GetLength(1);
        for (int i = 0; i < height; i++){
            grid[i, 0] = TileType.Wall;
            grid[i, width - 1] = TileType.Wall;
        }
        for (int i = 0; i < width; i++){
            grid[0, i] = TileType.Wall;
            grid[height-1, i] = TileType.Wall;
        }
    }

    public int[] GetCentroid(HashSet<int[]> room){
        int[] centroid = new int[2];
        int sumX = 0;
        int sumY = 0;
        foreach (int[] tile in room){
            sumX += tile[0];
            sumY += tile[1];
        }
        centroid[0] = (int)Mathf.Round(sumX / room.Count);
        centroid[1] = (int)Mathf.Round(sumY / room.Count);
        return centroid;
    }

    public Dictionary<int, int[]> GetRoomCentroids(Dictionary<int, HashSet<int[]>> rooms){
        Dictionary<int, int[]> centroids = new Dictionary<int, int[]>();
        foreach (KeyValuePair<int, HashSet<int[]>> room in rooms){
            centroids[room.Key] = GetCentroid(room.Value);
        }
        return centroids;
    }

    public void CreateBoxRooms(Dictionary<int, int[]> centroids, TileType[,] grid){
        int randomHeight;
        int randomWidth;
        foreach (KeyValuePair<int, int[]> centroid in centroids){
            randomHeight = Random.Range(4, 6);
            randomWidth = Random.Range(4, 6);
            for (int i = centroid.Value[0] - randomWidth; i < centroid.Value[0] + randomWidth; i++){
                for (int j = centroid.Value[1] - randomHeight; j < centroid.Value[1] + randomHeight; j++){
                    if (i > 0 && j > 0 && i < grid.GetLength(0) && j < grid.GetLength(1)){
                        grid[i, j] = TileType.Empty;
                    }
                }
            }
        }
    }

    public void CreateHallway(int[] start, int[] end, TileType[,] grid){
        int x = start[0];
        int y = start[1];
        int xEnd = end[0];
        int yEnd = end[1];
        while (x != xEnd || y != yEnd){
            if (x != xEnd){
                if (x < xEnd){
                    x++;
                } else{
                    x--;
                }
            }
            if (y != yEnd){
                if (y < yEnd){
                    y++;
                } else{
                    y--;
                }
            }
            grid[x, y] = TileType.Empty;
            grid[x+1, y] = TileType.Empty;
            grid[x-1, y] = TileType.Empty;
            grid[x, y+1] = TileType.Empty;
            grid[x, y-1] = TileType.Empty;
        }
    }
    public HashSet<int[]> FindHallways(Dictionary<int, int[]> centroids, float minDistance){
        HashSet<int[]> hallways = new HashSet<int[]>();
        HashSet<int[]> checkedCentroids = new HashSet<int[]>();
        foreach (KeyValuePair<int, int[]> centroid1 in centroids){
            foreach (KeyValuePair<int, int[]> centroid2 in centroids){
                float distance = Mathf.Sqrt(Mathf.Pow(centroid1.Value[0] - centroid2.Value[0], 2) + Mathf.Pow(centroid1.Value[1] - centroid2.Value[1], 2));
                if (distance < minDistance && !checkedCentroids.Contains(centroid2.Value)){
                    hallways.Add(new int[4]{centroid1.Value[0], centroid1.Value[1], centroid2.Value[0], centroid2.Value[1]});
                    checkedCentroids.Add(centroid2.Value);
                }
            }
        }
        return hallways;
    }

    public void CreateHallways(HashSet<int[]> hallways, TileType[,] grid){
        foreach (int[] hallway in hallways){
            CreateHallway(new int[2]{hallway[0], hallway[1]}, new int[2]{hallway[2], hallway[3]}, grid);
        }
    }

    public void LocateIndividualObjects(TileType[,] grid, Dictionary<int, int[]> centroids){
        List<int[]> justCoordinates = new List<int[]>();
        foreach (KeyValuePair<int, int[]> centroid in centroids){
            justCoordinates.Add(centroid.Value);
        }
        int randomCentroid = Random.Range(0, justCoordinates.Count);
        FillBlock(grid, justCoordinates[randomCentroid][1], justCoordinates[randomCentroid][0], 1, 1, TileType.Player);
        justCoordinates.RemoveAt(randomCentroid);
        randomCentroid = Random.Range(0, justCoordinates.Count);
        FillBlock(grid, justCoordinates[randomCentroid][1], justCoordinates[randomCentroid][0], 1, 1, TileType.Dagger);
        justCoordinates.RemoveAt(randomCentroid);

        randomCentroid = Random.Range(0, justCoordinates.Count);
        foreach(int[] pointAround in SquareAroundPoint(justCoordinates[randomCentroid][0], justCoordinates[randomCentroid][1])){
            FillBlock(grid, pointAround[1], pointAround[0], 1, 1, TileType.Door);
        }
        FillBlock(grid, justCoordinates[randomCentroid][1], justCoordinates[randomCentroid][0], 1, 1, TileType.End);
        justCoordinates.RemoveAt(randomCentroid);

        randomCentroid = Random.Range(0, justCoordinates.Count);
        FillBlock(grid, justCoordinates[randomCentroid][1], justCoordinates[randomCentroid][0], 1, 1, TileType.Enemy);
        justCoordinates.RemoveAt(randomCentroid);
        randomCentroid = Random.Range(0, justCoordinates.Count);
        FillBlock(grid, justCoordinates[randomCentroid][1], justCoordinates[randomCentroid][0], 1, 1, TileType.Key);
    }

    public HashSet<int[]> SquareAroundPoint(int x, int y){
        HashSet<int[]> square = new HashSet<int[]>();
        square.Add(new int[2]{x + 1, y});
        square.Add(new int[2]{x - 1, y});
        square.Add(new int[2]{x, y + 1});
        square.Add(new int[2]{x, y - 1});
        return square;
    }

}

