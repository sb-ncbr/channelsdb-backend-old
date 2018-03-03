
using ChannelsDB.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelsDB.Core.Utils
{
    public class PyMolExporter
    {
        StringBuilder Writer;

        int ColorIndex;
        static CultureInfo Culture = CultureInfo.InvariantCulture;
        static string[] Colors = new string[]
        {
            "red", "green", "blue", "yellow", "violet", "cyan", "salmon", "lime",
            "pink", "slate", "magenta", "orange", "marine", "olive", "purple", "teal",
            "forest", "firebrick", "chocolate", "wheat", "white", "grey"
        };

        public PyMolExporter(StringBuilder writer)
        {
            this.Writer = writer;
            writer.AppendLine("");
            writer.AppendLine("from pymol import cmd");
            writer.AppendLine("from pymol.cgo import *");
            writer.AppendLine("import chempy");

            new[]
            {
                "def addAtom(model, name, vdw, x, y, z, partialCharge = 0.0):",
                "  a = chempy.Atom()",
                "  a.name = name",
                "  a.vdw = vdw",
                "  a.coord = [x, y, z]",
                "  a.partial_charge = partialCharge",
                "  model.atom.append(a)"
            }.ToList().ForEach(l => writer.AppendLine(l));
        }

        public PyMolExporter AddTunnel(string commandName, Tunnel t)
        {
            Writer.AppendLine($"def {commandName}():");
            Writer.AppendLine("  model = chempy.models.Indexed()");
            var ctp = t.Profile;
            int index = 0;
            foreach (var n in ctp)
            {
                Writer.AppendLine(string.Format(Culture, "  addAtom(model,'{0}',{1:0.000},{2:0.000},{3:0.000},{4:0.000})", index, n.Radius, n.X, n.Y, n.Z));
                index++;
            }
            string nameString = "'" + commandName + "'";
            var color = "'" + Colors[(ColorIndex++) % Colors.Length] + "'";
            new[]
            {
                "for a in range(len(model.atom)-1):",
                "  b = chempy.Bond()",
                "  b.index = [a,a+1]",
                "  model.bond.append(b)",
                "cmd.set('surface_mode', 1)",
                "cmd.set('sphere_mode', 5)",
                "cmd.set('mesh_mode', 1)",
                string.Format("cmd.load_model(model, {0})", nameString),
                string.Format("cmd.hide('everything', {0})", nameString),
                string.Format("cmd.set('sphere_color', {0}, {1})", color, nameString),
                string.Format("cmd.show('spheres', {0})", nameString),
            }.ToList().ForEach(l => Writer.AppendLine("  " + l));
            Writer.AppendFormat("{0}()\n", commandName);
            Writer.AppendFormat("cmd.group('{0}', [{1}], 'add')", "Tunnels", commandName);
            Writer.AppendLine("");

            return this;
        }

        public PyMolExporter AddTunnels(List<Tunnel> tunnels)
        {
            for (int i = 0; i < tunnels.Count; i++)
            {
                AddTunnel($"Tunnel{i + 1}", tunnels.ElementAt(i));
            }

            return this;
        }
        public PyMolExporter AddFetch(string id)
        {
            Writer.AppendFormat("cmd.fetch('{0}')", id.ToUpperInvariant());
            return this;
        }
    }

    public class ChimeraExporter
    {
        #region Variables
        public StringBuilder Writer { get; set; }

        private static string[] Header = new string[] {
            "To run this script please open it in Chimera using 'Open' command.",
            "",
            "If you find this tool useful for your work, please cite it as:",
            "Sehnal D, Svobodová Vařeková R, Berka K, Pravda L, Navrátilová V, Banáš P, Ionescu C-M, Otyepka M, Koča J: MOLE 2.0: advanced approach for analysis of biomacromolecular channels. J. Cheminform. 2013, 5:39.",
            ""
        };

        private static Dictionary<string, string> Colors = new Dictionary<string, string> {
            {"red", "(1,0,0,.5)" },
            {"orange red", "(1,0.27,0,.5)" },
            {"orange", "(1,0.5,0,.5)" },
            {"yellow", "(1,1,0,.5)" },
            {"green", "(0,1,0,.5)" },
            {"forest green", "(0.13,0.54,0.13,.5)" },
            {"cyan", "(0,1,1,.5)" },
            {"light sea green", "(0.12,0.7,0.66,.5)" },
            {"blue", "(0,0,1,.5)" },
            {"cornflower blue", "(0.39,0.58,0.93,.5)" },
            {"medium blue", "(0.2,0.2,0.8,.5)" },
            {"purple", "(0.63,0.13,0.94,.5)" },
            {"hot pink", "(1,0.41,0.7,.5)" },
            {"magenta", "(1,0,1,.5)" },
            {"spring green", "(0,1,0.5,.5)" },
            {"plum", "(0.86,0.62,0.86,.5)" },
            {"sky blue", "(0.53,0.81,0.92,.5)" },
            {"goldenrod", "(0.85,0.65,0.13,.5)" },
            {"olive drab", "(0.42,0.56,0.13,.5)" },
            {"coral", "(1,0.5,0.31,.5)" },
            {"rosy brown", "(0.74,0.56,0.56,.5)" },
            {"slate gray", "(0.44,0.5,0.57,.5)" }};
        #endregion Variables

        #region Constructor

        public ChimeraExporter(StringBuilder writer, string[] header = null)
        {
            this.Writer = writer;

            new string[] {
                "import chimera, _surface, numpy",
                "from numpy import array, single as floatc, intc",
                "",
                "def addAtom(molecule, id, residue, x, y, z, radius):",
                "    at = molecule.newAtom(id, chimera.Element(\"Tunn\"))",
                "    at.setCoord(chimera.Coord(x,y,z))",
                "    at.radius = radius",
                "    residue.addAtom(at)",
                ""
            }.ToList().ForEach(x => Writer.AppendLine(x));
        }
        #endregion

        public ChimeraExporter AddFetch(string id)
        {
            Writer.AppendLine($"chimera.runCommand('open cifID:{id}')");
            return this;
        }

        public ChimeraExporter AddTunnels(List<Tunnel> tunnels)
        {
            for (int i = 0; i < tunnels.Count; i++) AddTunnel($"Tunnel{i + 1}", tunnels.ElementAt(i));
            for (int i = 0; i < tunnels.Count; i++) ShowRepresentation(i + 1);

            return this;
        }

        private void ShowRepresentation(int i)
        {
            Writer.AppendLine($"chimera.runCommand('color {Colors.ElementAt(i % Colors.Count).Key} #{i}')");
            Writer.AppendLine($"chimera.runCommand('repr cpk : {i}')");
        }

        public ChimeraExporter AddTunnel(string id, Tunnel t)
        {
            Writer.AppendLine($"def {id}(tunnelObject):");
            Writer.AppendLine($"    tunnel = tunnelObject.newResidue(\"{id}\", \" \", 1, \" \")");

            foreach (var item in t.Profile) Writer.AppendLine($"    addAtom(tunnelObject, \"{id}\", tunnel, {item.X:.00}, {item.Y:.00}, {item.Z:.00}, {item.Radius:.00})");

            Writer.AppendLine("tunnelObject = chimera.Molecule()");
            Writer.AppendLine("tunnelObject.name = \"{id}\"");

            Writer.AppendLine($"{id}(tunnelObject)");
            Writer.AppendLine("chimera.openModels.add([tunnelObject])\n");

            return this;
        }
    }


    public class VmdExporter
    {
        #region Variables
        public StringBuilder Writer { get; set; }

        private static string[] Header = new string[] {
            "To run this script please run it in VMD as source script.vmd",
            "",
            "If you find this tool useful for your work, please cite it as:",
            "Sehnal D, Svobodová Vařeková R, Berka K, Pravda L, Navrátilová V, Banáš P, Ionescu C-M, Otyepka M, Koča J: MOLE 2.0: advanced approach for analysis of biomacromolecular channels. J. Cheminform. 2013, 5:39.",
            ""
        };

        private static string[] Colors = new string[] {
            "blue","red","gray","orange","yellow","tan",
            "silver","green","white","pink","cyan","purple",
            "lime","mauve","ochre","iceblue","black","yellow2",
            "yellow3","green2","green3","cyan2","cyan3",
            "blue2","blue3","violet","violet2","magenta","magenta2",
            "red2","red3","orange2","orange3",
            };
        #endregion Variables


        #region Constructor
        public VmdExporter(StringBuilder writer, string[] header = null)
        {
            this.Writer = writer;

            writer.AppendLine("package require http");
            foreach (var l in header ?? Header)
            {
                writer.AppendLine("# " + l);
            }

            new string[] {
            "proc add_atom {id center r} {",
            " set atom [atomselect top \"index $id\"]",
            " $atom set {x y z}  $center",
            " $atom set radius $r",
            "}\n" }.ToList().ForEach(x => writer.AppendLine(x));
        }
        #endregion

        public VmdExporter AddFetch(string id)
        {
            new string[] {
                 "proc fetch_protein { url } {",
                 " set token [::http::geturl $url]",
                 " set data [::http::data $token]",
                 " ::http::cleanup $token",
                $" set fileName \"{id}.cif\"",
                 " set file [open $fileName \"w\"]",
                 " puts -nonewline $file $data",
                $" mol new \"{id}.cif\" type {{pdbx}}",
            "}\n" }.ToList().ForEach(x => Writer.AppendLine(x));

            Writer.AppendLine($"fetch_protein http://www.ebi.ac.uk/pdbe/entry-files/download/{id}.cif");
            return this;
        }

        public VmdExporter AddTunnels(List<Tunnel> tunnels, string name = "")
        {
            for (int i = 0; i < tunnels.Count; i++) AddTunnel(name, i + 1, tunnels.ElementAt(i));

            Writer.AppendLine("display resetview");

            return this;
        }


        public VmdExporter AddTunnel(string name, int id, Tunnel t)
        {

            Writer.AppendLine($"set tun{id} [mol new atoms {t.Profile.Count()}]");
            Writer.AppendLine($"mol top $tun{id}");
            Writer.AppendLine($"animate dup $tun{id}");
            Writer.AppendLine($"mol color ColorID {id % Colors.Count()}");
            Writer.AppendLine("mol representation VDW 1 60");

            for (int i = 0; i < t.Profile.Count(); i++)
            {
                Writer.AppendFormat("add_atom {0} {{{{ {1:0.00} {2:0.00} {3:0.00} }}}} {4:0.00}\n",
                    i, t.Profile[i].X, t.Profile[i].Y, t.Profile[i].Z, t.Profile[i].Radius);
            }

            Writer.AppendLine($"mol delrep 0 $tun{id}");
            Writer.AppendLine($"mol addrep $tun{id}");
            Writer.AppendLine("mol selection {{all}}");
            Writer.AppendFormat("mol rename top {{{0}{1}}}\n", (string.IsNullOrEmpty(name) ? "" : name + "_") + "Tunnel", id);
            Writer.AppendLine("");
            return this;
        }
    }

    public class PDBExporter
    {
        private StringBuilder sb;
        private string[] header = new string[] {
            "REMARK 920",
            "REMARK 920  This file was generated by MOLE 2 (http://mole.upol.cz, http://mole.chemi.muni.cz, version 2.5.17.4.24)",
            "REMARK 920",
            "REMARK 920  Please cite the following references when reporting the results using MOLE:",
            "REMARK 920   ",
            "REMARK 920  Sehnal D., Svobodova Varekova R., Berka K., Pravda L., Navratilova V., Banas P., Ionescu C.-M., Geidl S., Otyepka M., Koca J.:",
            "REMARK 920  MOLE 2.0: Advanced Approach for Analysis of Biomacromolecular Channels. Journal of Cheminformatics 2013, 5:39. doi:10.1186/1758-2946-5-39",
            "REMARK 920   ",
            "REMARK 920  and",
            "REMARK 920   ",
            "REMARK 920  Berka, K; Hanak, O; Sehnal, D; Banas, P; Navratilova, V; Jaiswal, D; Ionescu, C-M; Svobodova Varekova, R; Koca, J; Otyepka M:",
            "REMARK 920  MOLEonline 2.0: Interactive Web-based Analysis of Biomacromolecular Channels.Nucleic Acid Research, 2012, doi:10.1093/nar/GKS363",
            "REMARK 920   ",
            "REMARK ATOM  NAM RES   TUNID X       Y Z    Distnm RadiusA",
        };

        public PDBExporter(StringBuilder sb)
        {
            this.sb = sb;
            header.ToList().ForEach(x => sb.AppendLine(x));
        }

        public void AddTunnels(IEnumerable<Tunnel> tunnels)
        {
            var count = 0;
            for (int i = 1; i <= tunnels.Count(); i++)
            {
                var tunnel = tunnels.ElementAt(i - 1);

                for (int a = 1; a <= tunnel.Profile.Count(); a++)
                {
                    var node = tunnel.Profile.ElementAt(a - 1);
                    count++;
                    sb.AppendLine($"HETATM{count.ToString().PadLeft(5)}  X   TUN H{(a*i).ToString().PadLeft(4)}    {node.X.ToString("0.000").PadLeft(8)}{node.Y.ToString("0.000").PadLeft(8)}{node.Z.ToString("0.000").PadLeft(8)}{node.Distance.ToString("0.00").PadLeft(6)}{node.Radius.ToString("0.00").PadLeft(6)}");
                }
            }

        }

    }

}
