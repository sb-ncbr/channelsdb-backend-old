using System;
using System.Diagnostics;
using System.IO;

namespace ChannelsDB.Core.PyMOL
{
    public class PyMOL
    {
        private string pymol;

        public PyMOL(string pymolPath) {
            pymol = pymolPath;
        }

        public string[] GenerateVisualizationScript(string structurePath, string tunnelsPath, string resultPath) {
            var commands = new string[] {
                "from pymol import cmd",
                "import chempy",
                $"cmd.load('{structurePath.Replace("\\", "\\\\")}')",
                $"cmd.run('{tunnelsPath.Replace("\\", "\\\\")}')",
                "cmd.hide('all')",
                "cmd.show('cartoon')",
                "cmd.show('spheres', 'T*')",
                "cmd.show('sticks', 'het')",
                $"cmd.set('sphere_color', 'red', 'T*')",
                "cmd.set('cartoon_color', 'gray70')",
                "cmd.bg_color('white')",
                "cmd.set('ray_opaque_background', 0)",
                "cmd.set('cartoon_transparency', 0.3)",
                $"cmd.orient('{Path.GetFileNameWithoutExtension(structurePath)}')",
                $"cmd.png('{resultPath.Replace("\\", "\\\\")}', 200, 200, 300, 1)"
            };

            return commands;
        }


        public bool MakePicture(string scriptPath) {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo()
                {
                    FileName = pymol,
                    Arguments = $"-c {scriptPath}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                Process.Start(info).WaitForExit();
            }
            catch (Exception) { return false; }
            return true;
        }
    }
}
