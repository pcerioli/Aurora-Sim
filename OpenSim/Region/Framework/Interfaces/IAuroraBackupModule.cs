﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Serialization;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IAuroraBackupModule
    {
        bool IsArchiving { get; }
        void SaveModuleToArchive(TarArchiveWriter writer, IScene scene);

        void BeginLoadModuleFromArchive();

        void LoadModuleFromArchive(byte[] data, string filePath, TarArchiveReader.TarEntryType type, IScene scene);

        void EndLoadModuleFromArchive();
    }
}
