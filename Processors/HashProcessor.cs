﻿using Force.Crc32;
using NobleLauncher.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NobleLauncher.Processors
{
    static class HashProcessor
    {
        private static readonly int MAX_BUFFER_SIZE = 262144; //256 kb;
        public static async Task<string> CalcCRC32Hash(IUpdateable Patch, Action<long> OnBlockRead) {
            uint hash = 0u;
            var buffer = new byte[MAX_BUFFER_SIZE];

            if (!File.Exists(Patch.FullPath)) {
                return "";
            }

            await Task.Run(() => {
                using (var f = File.OpenRead(Patch.FullPath)) {
                    int count = 0;
                    while ((count = f.Read(buffer, 0, buffer.Length)) != 0) {
                        hash = Crc32Algorithm.Append(hash, buffer, 0, count);
                        OnBlockRead(count);
                    }
                }
            });

            return hash.ToString("X8");
        }
    }
}
