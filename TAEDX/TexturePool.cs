﻿using Microsoft.Xna.Framework.Graphics;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TAEDX
{
    public static class TexturePool
    {
        private static object _lock_IO = new object();
        private static object _lock_pool = new object();
        //This might be weird because it doesn't follow convention :fatcat:
        public delegate void TextureLoadErrorDelegate(string texName, string error);
        public static event TextureLoadErrorDelegate OnLoadError;
        private static void RaiseLoadError(string texName, string error)
        {
            OnLoadError?.Invoke(texName, error);
        }

        //private Dictionary<string, string> OnDemandTexturePaths = new Dictionary<string, string>();
        private static Dictionary<string, TextureFetchRequest> Fetches = new Dictionary<string, TextureFetchRequest>();

        public static void Flush()
        {
            lock (_lock_pool)
            {
                foreach (var fetch in Fetches)
                {
                    fetch.Value.Dispose();
                }
                Fetches.Clear();
            }
        }

        public static void AddFetch(TPF tpf, string texName)
        {
            string shortName = Path.GetFileNameWithoutExtension(texName);
            if (!Fetches.ContainsKey(shortName))
            {
                lock (_lock_pool)
                {
                    //if (tpf.Platform == TPF.TPFPlatform.PS3)
                    //{
                    //    tpf.ConvertPS3ToPC();
                    //}
                    //if (tpf.Platform == TPF.TPFPlatform.PS4)
                    //{
                    //    tpf.ConvertPS4ToPC();
                    //}
                    var newFetch = new TextureFetchRequest(tpf, texName);
                    Fetches.Add(shortName, newFetch);
                }
            }

        }

        public static void AddTpf(TPF tpf)
        {
            foreach (var tex in tpf.Textures)
            {
                AddFetch(tpf, tex.Name);
            }
        }

        public static void AddTextureBnd(IBinder bnd, IProgress<double> prog)
        {
            var tpfs = bnd.Files.Where(file => file.Name.EndsWith(".tpf")).ToList();
            var tbnds = bnd.Files.Where(file => file.Name.ToLower().EndsWith(".tbnd")).ToList();

            double total = tpfs.Count + tbnds.Count;
            double tpfFraction = 0;
            double tbndFraction = 0;
            if (total > 0)
            {
                tpfFraction = tpfs.Count / total;
                tbndFraction = tbnds.Count / total;
            }

            for (int i = 0; i < tpfs.Count; i++)
            {
                var file = tpfs[i];
                if (file.Bytes.Length > 0)
                {
                    TPF tpf = TPF.Read(file.Bytes);
                    AddTpf(tpf);
                }

                prog?.Report(i / tpfFraction);
            }

            for (int i = 0; i < tbnds.Count; i++)
            {
                var file = tbnds[i];
                if (file.Bytes.Length > 0)
                {
                    IBinder tbnd = BND3.Read(file.Bytes);
                    for (int j = 0; j < tbnd.Files.Count; j++)
                    {
                        TPF tpf = TPF.Read(tbnd.Files[j].Bytes);
                        AddTpf(tpf);

                        prog?.Report(tpfFraction + i / tbndFraction + j / tbnd.Files.Count * (tbndFraction / tbnds.Count));
                    }
                }

                prog?.Report(tpfFraction + i / tbndFraction);
            }

            prog?.Report(1);
        }

        public static void AddTpfFromPath(string path)
        {
            TPF tpf = SoulsFormats.TPF.Read(path);
            AddTpf(tpf);
        }

        public static Texture2D FetchTexture(string name)
        {
            if (name == null)
                return null;
            var shortName = Path.GetFileNameWithoutExtension(name);
            if (Fetches.ContainsKey(shortName))
            {
                lock (_lock_pool)
                {
                    return Fetches[shortName].Fetch();
                }
            }
            else
            {
                if (Fetches.ContainsKey(shortName + "_atlas000"))
                {
                    lock (_lock_pool)
                    {
                        return Fetches[shortName + "_atlas000"].Fetch();
                    }
                }
                return null;
            }
        }
    }
}