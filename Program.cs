// <copyright file="Program.cs" company="Sedat Kapanoglu">
// Copyright © 2016-2020 Sedat Kapanoglu
// SPDX-License-Identifier: MIT
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UtfUnknown;

namespace Unbom;

public static class Program
{
    private static readonly byte[] BOM = [0xEF, 0xBB, 0xBF];
    private static readonly Encoding UTF8WithoutBom = new UTF8Encoding(false);

    /// <summary>
    ///     Removes UTF-8 BOM markers from text files.
    /// </summary>
    /// <param name="argument">Path to scan.</param>
    /// <param name="recurse">recurse subdirectories.</param>
    /// <param name="nobackup">do not save a backup file.</param>
    public static void Main(string argument, bool recurse = false, bool nobackup = true)
    {
        Unbom(argument, recurse, nobackup);
    }

    public static void RemoveBom(string fileName, bool nobackup)
    {
        string tempName;

        if (HasBom(fileName))
            return;

        Console.Write($"{fileName}: BOM found - removing...");

        using FileStream stream = File.OpenRead(fileName);
        Span<byte> buffer = new byte[BOM.Length].AsSpan();
        _ = stream.Read(buffer);

        // GetTempFileName also creates the file
        string tempFileName = Path.GetTempFileName();
        using FileStream outputStream = File.Create(tempName = tempFileName);
        stream.CopyTo(outputStream);

        string backupName = fileName + ".bak";
        File.Move(fileName, backupName, true);
        File.Move(tempName, fileName);

        if (nobackup)
            File.Delete(backupName);

        Console.WriteLine("done");
    }

    private static void Unbom(string path, bool recurse = false, bool noBackup = false)
    {
        Debug.WriteLine($"path={path} recurse={recurse} nobackup={noBackup}");

        string pattern = Path.GetFileName(path);

        if (string.IsNullOrEmpty(pattern))
            pattern = "*";
        else
            path = Path.GetDirectoryName(path) ?? ".";

        Debug.WriteLine($"path={path} pattern={pattern}");

        try
        {
            IEnumerable<string> files = Directory.EnumerateFiles(path, pattern, recurse
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly);

            var count = 0;

            foreach (string fileName in files)
            {
                //RemoveBom(fileName, noBackup);
                ToUtfNoBom(fileName);
                count++;
            }

            Console.WriteLine($"{count} file(s) processed");
        }
        catch (DirectoryNotFoundException)
        {
            Console.Error.WriteLine($"Directory not found: {path}");
        }
    }

    private static void ToUtfNoBom(string fileName)
    {
        DetectionResult result = CharsetDetector.DetectFromFile(fileName);
        DetectionDetail resultDetected = result.Detected;

        string encodingName = resultDetected?.EncodingName;
        bool isUtf8 = encodingName is "ascii" or "utf8" or "utf-8";
        bool hasBOM = isUtf8 && resultDetected.HasBOM || HasBom(fileName);

        if (isUtf8 && !hasBOM)
            return;

        try
        {
            var message =
                $"{(!string.IsNullOrEmpty(encodingName) ? $"{encodingName}{(hasBOM ? " BOM" : string.Empty)} found" : "unknown")} - converting: {fileName} ";

            Console.Write(message);

            string content = File.ReadAllText(fileName);
            File.WriteAllText(fileName, content, UTF8WithoutBom);
            Console.WriteLine("done");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static bool HasBom(string fileName)
    {
        Span<byte> buffer = new byte[BOM.Length].AsSpan();
        using FileStream stream = File.OpenRead(fileName);

        int bytesRead = stream.Read(buffer);

        return bytesRead != buffer.Length || !buffer.SequenceEqual(BOM);
    }
}