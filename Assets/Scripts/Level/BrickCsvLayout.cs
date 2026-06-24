using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct BrickCellData
{
    public readonly bool IsEmpty;
    public readonly char TypeCode;
    public readonly int DirectionIndex;
    public readonly string CollisionNote;

    public BrickCellData(char typeCode, int directionIndex, string collisionNote)
    {
        IsEmpty = false;
        TypeCode = typeCode;
        DirectionIndex = directionIndex;
        CollisionNote = collisionNote;
    }

    private BrickCellData(bool isEmpty)
    {
        IsEmpty = isEmpty;
        TypeCode = '\0';
        DirectionIndex = 0;
        CollisionNote = string.Empty;
    }

    public static BrickCellData Empty => new(true);
}

public class BrickCsvLayout
{
    readonly BrickCellData[][] _rows;

    public int RowCount => _rows.Length;
    public int ColumnCount { get; }

    public BrickCsvLayout(BrickCellData[][] rows, int columnCount)
    {
        _rows = rows;
        ColumnCount = columnCount;
    }

    public BrickCellData GetCell(int row, int column)
    {
        if (row < 0 || row >= _rows.Length)
        {
            return BrickCellData.Empty;
        }

        if (column < 0 || column >= _rows[row].Length)
        {
            return BrickCellData.Empty;
        }

        return _rows[row][column];
    }
}

public static class BrickCsvParser
{
    public static bool TryParse(TextAsset csvFile, out BrickCsvLayout layout, out string error)
    {
        layout = null;
        error = string.Empty;

        if (csvFile == null)
        {
            error = "No CSV level file was assigned.";
            return false;
        }

        string[] lines = csvFile.text
            .Replace("\r", string.Empty)
            .Split('\n');

        int lastContentLine = lines.Length - 1;

        while (lastContentLine >= 0 &&
               string.IsNullOrWhiteSpace(lines[lastContentLine]))
        {
            lastContentLine--;
        }

        if (lastContentLine < 0)
        {
            error = "The CSV level file is empty.";
            return false;
        }

        List<BrickCellData[]> rows = new();
        int maxColumns = 0;

        for (int row = 0; row <= lastContentLine; row++)
        {
            string line = lines[row];

            if (string.IsNullOrWhiteSpace(line))
            {
                error = $"CSV row {row + 1} is empty.";
                return false;
            }

            string[] rawCells = line.Split(',');
            BrickCellData[] parsedCells = new BrickCellData[rawCells.Length];

            for (int column = 0; column < rawCells.Length; column++)
            {
                if (!TryParseCell(rawCells[column], out parsedCells[column], out string cellError))
                {
                    error = $"CSV row {row + 1}, column {column + 1}: {cellError}";
                    return false;
                }
            }

            rows.Add(parsedCells);
            maxColumns = Mathf.Max(maxColumns, parsedCells.Length);
        }

        layout = new BrickCsvLayout(rows.ToArray(), maxColumns);
        return true;
    }

    static bool TryParseCell(string rawCell, out BrickCellData cell, out string error)
    {
        cell = BrickCellData.Empty;
        error = string.Empty;

        string token = rawCell.Trim();

        if (string.IsNullOrEmpty(token) || token == "." || token == "-")
        {
            return true;
        }

        // The first char is the brick. 
        // The second char is the direction.
        // The third and fourth char is the musical note. Each brick was meant to be tuned to a specific note, but I ran out of time. 
        
        // Minimum: A0C
        // Maximum: A0C#
        if (token.Length < 3 || token.Length > 4)
        {
            error = $"'{token}' must be formatted like A0C or A0C#.";
            return false;
        }

        char typeCode = char.ToUpperInvariant(token[0]);
        char directionCharacter = token[1];

        if (!char.IsLetter(typeCode))
        {
            error = $"'{token}' has an invalid brick type code.";
            return false;
        }

        if (directionCharacter < '0' || directionCharacter > '7')
        {
            error = $"'{token}' must use a direction between 0 and 7.";
            return false;
        }

        string noteToken = token.Substring(2);

        if (!TryParseNote(noteToken, out string note, out string noteError))
        {
            error = $"'{token}' has an invalid note: {noteError}";
            return false;
        }

        cell = new BrickCellData(
            typeCode,
            directionCharacter - '0',
            note
        );

        return true;
    }

    static bool TryParseNote(
        string rawNote,
        out string note,
        out string error)
    {
        note = string.Empty;
        error = string.Empty;

        if (rawNote.Length < 1 || rawNote.Length > 2)
        {
            error = "note must be A-G, optionally followed by #.";
            return false;
        }

        char rootNote = char.ToUpperInvariant(rawNote[0]);

        if ("CDEFGAB".IndexOf(rootNote) < 0)
        {
            error = "note must begin with A, B, C, D, E, F, or G.";
            return false;
        }

        if (rawNote.Length == 2 && rawNote[1] != '#')
        {
            error = "the only supported accidental is #.";
            return false;
        }

        note = rawNote.Length == 2
            ? $"{rootNote}#"
            : rootNote.ToString();

        return true;
    }
}