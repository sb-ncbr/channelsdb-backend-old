using System;
using System.Collections.Generic;
using System.Text;

namespace ChannelsDB.Update
{
    public class Config
    {
        public string DbRepository { get; set; }
        public string ExcludedEntries { get; set; }
        public string ApiRoot { get; set; }
        public string PdbRepository { get; set; }
        public string PyMOL { get; set; }
    }

    public class APIResult {
        public string Date => DateTime.Today.ToString().Split(new char[0])[0];
        public int Reviewed { get; set; }
        public int CSA { get; set; }
        public int Cofactors { get; set; }
        public int Pores { get; set; }
        public int Total { get; set; }
    }
}
