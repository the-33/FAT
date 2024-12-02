﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAT.Data
{
    internal class File : Cluster
    {
        public byte[] Data { get; set; }

        public File(int clusterSize)
        {
            Data = new byte[clusterSize];
        }
    }
}
