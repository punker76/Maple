﻿using System.Diagnostics;
using System.IO;

namespace Maple.Core
{
    [DebuggerDisplay("Directory: {Name} IsContainer: {IsContainer}")]
    public class MapleDirectory : MapleFileSystemContainerBase, IFileSystemDirectory
    {

        public MapleDirectory(DirectoryInfo info, IDepth depth, IFileSystemDirectory parent) : base(info.Name, info.FullName, depth, parent)
        {
            using (_busyStack.GetToken())
            {
                if (!Depth.IsMaxReached)
                    Refresh();
            }
        }

        public override void LoadMetaData()
        {
            var info = new DirectoryInfo(FullName);

            Exists = info.Exists;
            IsHidden = info.Attributes.HasFlag(FileAttributes.Hidden);
        }

        public override void Delete()
        {
            Directory.Delete(FullName);
            Refresh();
        }

        public override bool CanDelete()
        {
            return Directory.Exists(FullName);
        }
    }
}
