
using System;
using System.Collections.Generic;
using System.IO;
using Sys = Cosmos.System;

public class Kernel : Sys.Kernel
{
    Sys.FileSystem.CosmosVFS fs;



    protected override void BeforeRun()
    {
        fs = new Sys.FileSystem.CosmosVFS();
        Sys.FileSystem.VFS.VFSManager.RegisterVFS(fs);
    }
    protected override void Run()
    {
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Clear();

        Console.Write("File name: ");
        string filename ="0:\\" + Console.ReadLine();

        if (filename == null || filename.Length == 0)
            filename = "text.txt";

        List<string> lines = new List<string>();

        // Load existing file or create an empty document.
        if (File.Exists(filename))
        {
            string content = File.ReadAllText(filename);
            string[] parts = content.Split('\n');

            for (int i = 0; i < parts.Length; i++)
            {
                string line = parts[i];

                if (line.EndsWith("\r"))
                    line = line.Substring(0, line.Length - 1);

                lines.Add(line);
            }
        }
        else
        {
            lines.Add("");
        }

        if (lines.Count == 0)
            lines.Add("");

        Edit(filename, lines);
    }

    private void Edit(string filename, List<string> lines)
    {
        int cx = 0;
        int cy = 0;

        int scrollY = 0;
        int scrollX = 0;

        string message = "";

        Console.CursorVisible = true;

        while (true)
        {
            int width = Console.WindowWidth;
            int height = Console.WindowHeight;

            if (width < 2) width = 2;
            if (height < 4) height = 4;

            int screenRows = height - 2;
            int screenWidth = width - 1;

            // Adjust vertical scrolling.
            if (cy < scrollY)
                scrollY = cy;

            if (cy >= scrollY + screenRows)
                scrollY = cy - screenRows + 1;

            // Adjust horizontal scrolling.
            if (cx < scrollX)
                scrollX = cx;

            if (cx >= scrollX + screenWidth)
                scrollX = cx - screenWidth + 1;

            // Draw header.
            Console.SetCursorPosition(0, 0);
            WriteScreenLine(" COSMOS NANO - " + filename, width);

            // Draw document.
            for (int row = 0; row < screenRows; row++)
            {
                Console.SetCursorPosition(0, row + 1);

                int lineIndex = scrollY + row;

                if (lineIndex < lines.Count)
                {
                    string line = lines[lineIndex];

                    if (scrollX < line.Length)
                    {
                        string visible = line.Substring(scrollX);

                        if (visible.Length > screenWidth)
                            visible = visible.Substring(0, screenWidth);

                        WriteScreenLine(visible, width);
                    }
                    else
                    {
                        WriteScreenLine("", width);
                    }
                }
                else
                {
                    WriteScreenLine("", width);
                }
            }

            // Draw status line.
            Console.SetCursorPosition(0, height - 1);

            string status =
                " ESC: Exit | Ln " + (cy + 1) +
                " Col " + (cx + 1) + " | " + message;

            WriteScreenLine(status, width);

            message = "";

            // Position cursor.
            int cursorX = cx - scrollX;
            int cursorY = cy - scrollY + 1;

            if (cursorX >= screenWidth)
                cursorX = screenWidth - 1;

            if (cursorY >= height - 1)
                cursorY = height - 2;

            Console.SetCursorPosition(cursorX, cursorY);

            ConsoleKeyInfo key = Console.ReadKey(true);

            // ESC - Exit and ask whether to save.
            if (key.Key == ConsoleKey.Escape)
            {
                Console.SetCursorPosition(0, height - 1);
                WriteScreenLine(" Save file? Y = Yes / N = No", width);

                ConsoleKeyInfo answer = Console.ReadKey(true);

                if (answer.Key == ConsoleKey.Y)
                {
                    File.WriteAllText(filename, JoinLines(lines));
                    Console.Clear();
                    Console.CursorVisible = true;
                    Console.WriteLine("File saved: " + filename);
                    return;
                }

                if (answer.Key == ConsoleKey.N)
                {
                    Console.Clear();
                    Console.CursorVisible = true;
                    Console.WriteLine("Editor closed.");
                    return;
                }

                message = "Press Y or N";
                continue;
            }

            // Arrow keys.
            if (key.Key == ConsoleKey.LeftArrow)
            {
                if (cx > 0)
                    cx--;
                else if (cy > 0)
                {
                    cy--;
                    cx = lines[cy].Length;
                }
            }
            else if (key.Key == ConsoleKey.RightArrow)
            {
                if (cx < lines[cy].Length)
                    cx++;
                else if (cy < lines.Count - 1)
                {
                    cy++;
                    cx = 0;
                }
            }
            else if (key.Key == ConsoleKey.UpArrow)
            {
                if (cy > 0)
                {
                    cy--;
                    if (cx > lines[cy].Length)
                        cx = lines[cy].Length;
                }
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                if (cy < lines.Count - 1)
                {
                    cy++;
                    if (cx > lines[cy].Length)
                        cx = lines[cy].Length;
                }
            }

            // Enter - insert a new line.
            else if (key.Key == ConsoleKey.Enter)
            {
                string current = lines[cy];

                string left = current.Substring(0, cx);
                string right = current.Substring(cx);

                lines[cy] = left;
                lines.Insert(cy + 1, right);

                cy++;
                cx = 0;
            }

            // Backspace.
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (cx > 0)
                {
                    string current = lines[cy];

                    lines[cy] =
                        current.Substring(0, cx - 1) +
                        current.Substring(cx);

                    cx--;
                }
                else if (cy > 0)
                {
                    string previous = lines[cy - 1];
                    string current = lines[cy];

                    cx = previous.Length;

                    lines[cy - 1] = previous + current;
                    lines.RemoveAt(cy);

                    cy--;
                }
            }

            // Delete.
            else if (key.Key == ConsoleKey.Delete)
            {
                if (cx < lines[cy].Length)
                {
                    string current = lines[cy];

                    lines[cy] =
                        current.Substring(0, cx) +
                        current.Substring(cx + 1);
                }
                else if (cy < lines.Count - 1)
                {
                    lines[cy] = lines[cy] + lines[cy + 1];
                    lines.RemoveAt(cy + 1);
                }
            }

            // Insert printable characters.
            else if (!char.IsControl(key.KeyChar))
            {
                string current = lines[cy];

                lines[cy] =
                    current.Substring(0, cx) +
                    key.KeyChar +
                    current.Substring(cx);

                cx++;
            }
        }
    }

    // Draw one screen line without overflowing the console.
    private void WriteScreenLine(string text, int width)
    {
        int limit = width - 1;

        if (limit < 1)
            limit = 1;

        if (text.Length > limit)
            text = text.Substring(0, limit);

        Console.Write(text);

        int spaces = limit - text.Length;

        for (int i = 0; i < spaces; i++)
            Console.Write(" ");
    }

    // Convert document lines into a single string.
    private string JoinLines(List<string> lines)
    {
        string result = "";

        for (int i = 0; i < lines.Count; i++)
        {
            result += lines[i];

            if (i < lines.Count - 1)
                result += "\n";
        }

        return result;
    }
}
