using System;
using System.IO;

try
{
    using var game = new MyGame.Game1();
    game.Run();
}
catch (Exception ex)
{
    File.WriteAllText("crash_report.txt", ex.ToString());
    throw;
}
