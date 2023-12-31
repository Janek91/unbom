// <copyright file="Program.cs" company="Sedat Kapanoglu">
// Copyright © 2016-2020 Sedat Kapanoglu
// SPDX-License-Identifier: MIT
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UtfUnknown;

namespace Unbom;

public static class Program
{
    private static readonly byte[] BOM = [0xEF, 0xBB, 0xBF];
    private static readonly Encoding UTF8WithoutBom = new UTF8Encoding(false);
    private static readonly Encoding UTF8WithBom = new UTF8Encoding(true);

    /// <summary>
    /// Removes UTF-8 BOM markers from text files.
    /// </summary>
    /// <param name="argument">Path to scan.</param>
    /// <param name="recurse">recurse subdirectories.</param>
    /// <param name="nobackup">do not save a backup file.</param>
    /// <param name="setBOM">Add BOM marker</param>
    public static void Main(string argument, bool recurse = false, bool nobackup = true, bool setBOM = false)
    {
        Unbom(argument, recurse, nobackup, setBOM);
    }

    public static void RemoveBom(string fileName, bool nobackup)
    {
        string tempName;

        if (HasBom(fileName))
            return;

        Console.Write($"{fileName}: BOM found - removing...");

        using var stream = File.OpenRead(fileName);
        var buffer = new byte[BOM.Length].AsSpan();
        _ = stream.Read(buffer);

        // GetTempFileName also creates the file
        var tempFileName = Path.GetTempFileName();
        using var outputStream = File.Create(tempName = tempFileName);
        stream.CopyTo(outputStream);

        var backupName = fileName + ".bak";
        File.Move(fileName, backupName, true);
        File.Move(tempName, fileName);

        if (nobackup)
            File.Delete(backupName);

        Console.WriteLine("done");
    }

    private static void Unbom(string path, bool recurse, bool noBackup, bool setBOM)
    {
        Debug.WriteLine($"path={path} recurse={recurse} nobackup={noBackup} setBOM={setBOM}");

        var pattern = Path.GetFileName(path);

        if (string.IsNullOrEmpty(pattern))
            pattern = "*";
        else
            path = Path.GetDirectoryName(path) ?? ".";

        Debug.WriteLine($"path={path} pattern={pattern}");

        try
        {
            var files = Directory.EnumerateFiles(path, pattern, recurse
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly);

            var count = 0;

            foreach (var fileName in files)
            {
                //RemoveBom(fileName, noBackup);
                ToUtfNoBom(fileName, setBOM,ref count);
            }

            Console.WriteLine($"{count} file(s) processed");
        }
        catch (DirectoryNotFoundException)
        {
            Console.Error.WriteLine($"Directory not found: {path}");
        }
    }

    private static void ToUtfNoBom(string fileName, bool setBOM, ref int count)
    {
        var result = CharsetDetector.DetectFromFile(fileName);
        var resultDetected = result.Detected;

        var encodingName = resultDetected?.EncodingName;
        var isUtf8 = encodingName is "ascii" or "utf8" or "utf-8";
        var hasBOM = (isUtf8 && resultDetected.HasBOM) || HasBom(fileName);

        if (isUtf8 && ((setBOM && hasBOM) || (!setBOM && !hasBOM)))
            return;

        try
        {
            var message =
                $"{(!string.IsNullOrEmpty(encodingName) ? $"{encodingName}{(hasBOM ? " BOM" : string.Empty)} found" : "unknown")} - converting: {fileName} ";

            Console.Write(message);

            var content = File.ReadAllText(fileName);
            File.WriteAllText(fileName, content, setBOM ? UTF8WithBom : UTF8WithoutBom);
            Console.WriteLine("done");
            count++;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static bool HasBom(string fileName)
    {
        var buffer = new byte[BOM.Length].AsSpan();
        using var stream = File.OpenRead(fileName);

        var bytesRead = stream.Read(buffer);

        return bytesRead == buffer.Length && buffer.SequenceEqual(BOM);
    }
}