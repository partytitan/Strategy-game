﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    [SerializeField]
    private HexCell[] neighbors;

    [SerializeField]
    private bool[] roads;

    public HexCoordinates coordinates;
    public RectTransform uiRect;
    public HexGridChunk chunk;

    private int _elevation = int.MinValue;
    private int _terrainTypeIndex;
    private int _waterLevel;
    private int _urbanLevel, _farmLevel, _plantLevel;
    private int _specialIndex;
    bool _walled;



    private bool _hasIncomingRiver, _hasOutgoingRiver;
    private HexDirection _incomingRiver, _outgoingRiver;

    public bool HasIncomingRiver => _hasIncomingRiver;

    public bool HasOutgoingRiver => _hasOutgoingRiver;

    public HexDirection IncomingRiver => _incomingRiver;

    public HexDirection OutgoingRiver => _outgoingRiver;

    public bool HasRiver => _hasIncomingRiver || _hasOutgoingRiver;

    public bool HasRiverBeginOrEnd => _hasIncomingRiver != _hasOutgoingRiver;

    public float StreamBedY =>
        (Elevation + HexMetrics.StreamBedElevationOffset) *
        HexMetrics.ElevationStep;

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return
            _hasIncomingRiver && _incomingRiver == direction ||
            _hasOutgoingRiver && _outgoingRiver == direction;
    }


    public bool HasRoads
    {
        get
        {
            
            foreach (var road in roads)
            {
                if (road)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public int WaterLevel
    {
        get => _waterLevel;
        set
        {
            if (_waterLevel == value)
            {
                return;
            }
            _waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }

    public int UrbanLevel
    {
        get => _urbanLevel;
        set
        {
            if (_urbanLevel != value)
            {
                _urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel
    {
        get => _farmLevel;
        set
        {
            if (_farmLevel != value)
            {
                _farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel
    {
        get => _plantLevel;
        set
        {
            if (_plantLevel != value)
            {
                _plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public Color Color
    {
        get => HexMetrics.Colors[_terrainTypeIndex];
    }
    public int TerrainTypeIndex
    {
        get => _terrainTypeIndex;
        set
        {
            if (_terrainTypeIndex != value)
            {
                _terrainTypeIndex = value;
                Refresh();
            }
        }
    }

    public bool Walled
    {
        get => _walled;
        set
        {
            if (_walled != value)
            {
                _walled = value;
                Refresh();
            }
        }
    }


    public float RiverSurfaceY => (_elevation + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;
    public float WaterSurfaceY => (_waterLevel + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;

    public int Elevation
    {
        get => _elevation;
        set
        {
            if (_elevation == value)
            {
                return;
            }

            _elevation = value;
            RefreshPosition();
            ValidateRivers();

            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
                {
                    SetRoad(i, false);
                }
            }


            Refresh();
        }
    }

    public bool IsSpecial
    {
        get
        {
            return _specialIndex > 0;
        }
    }

    public int SpecialIndex
    {
        get
        {
            return _specialIndex;
        }
        set
        {
            if (_specialIndex != value && !HasRiver)
            {
                _specialIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }

    public Vector3 Position => transform.localPosition;

    public HexDirection RiverBeginOrEndDirection => _hasIncomingRiver ? _incomingRiver : _outgoingRiver;

    public bool IsUnderwater => _waterLevel > _elevation;

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(_elevation, neighbors[(int)direction].Elevation
        );
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(Elevation, otherCell.Elevation);
    }

    public void RemoveOutgoingRiver()
    {
        if (!_hasOutgoingRiver)
        {
            return;
        }
        _hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(_outgoingRiver);
        neighbor._hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver()
    {
        if (!_hasIncomingRiver)
        {
            return;
        }
        _hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(_incomingRiver);
        neighbor._hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void SetOutgoingRiver(HexDirection direction)
    {
        if (HasOutgoingRiver && _outgoingRiver == direction)
        {
            return;
        }

        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor))
        {
            return;
        }

        RemoveOutgoingRiver();
        if (_hasIncomingRiver && _incomingRiver == direction)
        {
            RemoveIncomingRiver();
        }

        _hasOutgoingRiver = true;
        _outgoingRiver = direction;
        _specialIndex = 0;

        neighbor.RemoveIncomingRiver();
        neighbor._hasIncomingRiver = true;
        neighbor._incomingRiver = direction.Opposite();
        neighbor._specialIndex = 0;

        SetRoad((int)direction, false);
    }

    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int)direction];
    }
    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (roads[i])
            {
                SetRoad(i, false);
            }
        }
    }

    public int GetElevationDifference(HexDirection direction)
    {
        int difference = Elevation - GetNeighbor(direction).Elevation;
        return difference >= 0 ? difference : -difference;
    }

    public void AddRoad(HexDirection direction)
    {
        if (!roads[(int)direction] && !HasRiverThroughEdge(direction) &&
            !IsSpecial && !GetNeighbor(direction).IsSpecial &&
            GetElevationDifference(direction) <= 1)
        {
            SetRoad((int)direction, true);
        }
    }


    private void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    private bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor && (
                   _elevation >= neighbor._elevation || _waterLevel == neighbor._elevation
               );
    }

    private void ValidateRivers()
    {
        if (
            _hasOutgoingRiver &&
            !IsValidRiverDestination(GetNeighbor(_outgoingRiver))
        )
        {
            RemoveOutgoingRiver();
        }
        if (
            _hasIncomingRiver &&
            !GetNeighbor(_incomingRiver).IsValidRiverDestination(this)
        )
        {
            RemoveIncomingRiver();
        }
    }
    private void Refresh()
    {
        if (chunk)
        {
            chunk.Refresh();
            foreach (var neighbor in neighbors)
            {
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }

    private void RefreshSelfOnly()
    {
        chunk.Refresh();
    }

    private void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = _elevation * HexMetrics.ElevationStep;
        position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)_terrainTypeIndex);
        writer.Write((byte)_elevation);
        writer.Write((byte)_waterLevel);
        writer.Write((byte)_urbanLevel);
        writer.Write((byte)_farmLevel);
        writer.Write((byte)_plantLevel);
        writer.Write((byte)_specialIndex);
        writer.Write(_walled);

        if (_hasIncomingRiver)
        {
            writer.Write((byte)(_incomingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }

        if (_hasOutgoingRiver)
        {
            writer.Write((byte)(_outgoingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }

        int roadFlags = 0;
        for (var i = 0; i < roads.Length; i++)
        {
            if (roads[i])
            {
                roadFlags |= 1 << i;
            }
        }
        writer.Write((byte)roadFlags);
    }

    public void Load(BinaryReader reader)
    {
        _terrainTypeIndex = reader.ReadByte();
        _elevation = reader.ReadByte();
        RefreshPosition();
        _waterLevel = reader.ReadByte();
        _urbanLevel = reader.ReadByte();
        _farmLevel = reader.ReadByte();
        _plantLevel = reader.ReadByte();
        _specialIndex = reader.ReadByte();
        _walled = reader.ReadBoolean();

        byte riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            _hasIncomingRiver = true;
            _incomingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            _hasIncomingRiver = false;
        }
        riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            _hasOutgoingRiver = true;
            _outgoingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            _hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++)
        {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }
    }
}
