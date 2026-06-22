using System; using System.Reflection; using Microsoft.AspNetCore.Authentication.Google; foreach (var type in typeof(GoogleDefaults).Assembly.GetExportedTypes()) { Console.WriteLine(type.FullName); }
