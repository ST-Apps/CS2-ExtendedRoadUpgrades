using System;
using System.IO;
using System.Text;
using System.Linq;

string[] args = Environment.GetCommandLineArgs();

if (args.Length < 3) {
    Console.WriteLine("Usage: rename.csx <from> <to>");
    return;
}

var from = args[2];
var to = args[3];
Console.WriteLine($"Parameters - From: {from}, To: {to}");

string[] directoriesToIgnore = {
    ".git",
    "dist",
    "obj",
    "ts-build",
};

var files = Directory.GetFiles(Environment.CurrentDirectory, "*.*", SearchOption.AllDirectories);
Console.WriteLine("Starting file processing...");

foreach (var file in files) {
    Console.WriteLine($"Processing file: {file}");

    // Check if the file's directory is in the ignore list
    string directory = Path.GetDirectoryName(file);
    if (directoriesToIgnore.Any(dir => file.StartsWith(Path.GetFullPath(dir)))) {
        Console.WriteLine($"Skipped file in ignored directory: {file}");
        continue;
    }

    // Replace text inside files
    var content = File.ReadAllText(file, Encoding.UTF8);
    if (content.Contains(from)) {
        content = content.Replace(from, to);
        File.WriteAllText(file, content, Encoding.UTF8);
        Console.WriteLine($"Replaced content in {file}");
    }

    // Rename files
    var fileName = Path.GetFileName(file);
    if (fileName.Contains(from)) {
        var newFileName = fileName.Replace(from, to);
        var newFilePath = Path.Combine(directory, newFileName);
        File.Move(file, newFilePath);
        Console.WriteLine($"Renamed file from {fileName} to {newFileName}");
    }
}

Console.WriteLine("Processing completed.");