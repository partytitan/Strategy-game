using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int cellCountX = 20, cellCountZ = 15;



    public HexCell cellPrefab;
    public Text cellLabelPrefab;

    public Texture2D noiseSource;

    public HexGridChunk chunkPrefab;

    public int seed;

    private HexCell[] _cells;
    private HexGridChunk[] _chunks;

    private int _chunkCountX, _chunkCountZ;

    public Color[] colors;



    private void Awake()
    {
        HexMetrics.NoiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexMetrics.Colors = colors;
        CreateMap(cellCountX, cellCountZ);
    }


    private void OnEnable()
    {
        if (!HexMetrics.NoiseSource)
        {
            HexMetrics.NoiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
        }
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return _cells[index];
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if (z < 0 || z >= cellCountZ)
        {
            return null;
        }
        int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX)
        {
            return null;
        }
        return _cells[x + z * cellCountX];
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i].ShowUI(visible);
        }
    }

    public bool CreateMap(int x, int z)
    {
        if (
            x <= 0 || x % HexMetrics.ChunkSizeX != 0 ||
            z <= 0 || z % HexMetrics.ChunkSizeZ != 0
        )
        {
            Debug.LogError("Unsupported map size.");
            return false;
        }

        if (_chunks != null)
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                Destroy(_chunks[i].gameObject);
            }
        }

        cellCountX = x;
        cellCountZ = z;
        _chunkCountX = cellCountX / HexMetrics.ChunkSizeX;
        _chunkCountZ = cellCountZ / HexMetrics.ChunkSizeZ;

        CreateChunks();
        CreateCells();

        return true;
    }

    private void CreateChunks()
    {
        _chunks = new HexGridChunk[_chunkCountX * _chunkCountZ];

        for (int z = 0, i = 0; z < _chunkCountZ; z++)
        {
            for (int x = 0; x < _chunkCountX; x++)
            {
                HexGridChunk chunk = _chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    private void CreateCells()
    {
        _cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void CreateCell(int x, int z, int i)
    {
        // By subtrackting the interger division of z we push every other row back
        // This keeps it a rectangular grid
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.OuterRadius * 1.5f);

        var cell = _cells[i] = Instantiate<HexCell>(cellPrefab);
        var cellTransform = cell.transform;
        cellTransform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        // Set cell neigbors
        if (x > 0)
        {
            // E to W cells
            cell.SetNeighbor(HexDirection.W, _cells[i - 1]);
        }
        // First row has no connections
        if (z > 0)
        {
            // All even rows have NW to SE connections
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, _cells[i - cellCountX]);
                // Connect to SW neigbor if not first in row
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, _cells[i - cellCountX - 1]);
                }
            }
            // All uneven rows have NW to SW connections
            else
            {
                cell.SetNeighbor(HexDirection.SW, _cells[i - cellCountX]);
                // Connect to SE neigbor if not last in row
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - cellCountX + 1]);
                }
            }
        }

        var label = Instantiate<Text>(cellLabelPrefab);
        var rextLabelTransform = label.rectTransform;
        rextLabelTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();
        cell.uiRect = label.rectTransform;

        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    private void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.ChunkSizeX;
        int chunkZ = z / HexMetrics.ChunkSizeZ;
        HexGridChunk chunk = _chunks[chunkX + chunkZ * _chunkCountX];

        int localX = x - chunkX * HexMetrics.ChunkSizeX;
        int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);

        foreach (var cell in _cells)
        {
            cell.Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header)
    {
        int x = 20, z = 15;
        if (header >= 1)
        {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        if (x != cellCountX || z != cellCountZ)
        {
            if (!CreateMap(x, z))
            {
                return;
            }
        }

        foreach (var cell in _cells)
        {
            cell.Load(reader);
        }

        foreach (var chunk in _chunks)
        {
            chunk.Refresh();
        }
    }
}
