//
// Copyright (c) 2008-2011, Kenneth Bell
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using DiscUtils.Internal;
using DiscUtils.Vfs;

namespace DiscUtils.SquashFs;

internal class Directory : File, IVfsDirectory<DirectoryEntry, File>
{
    private readonly IDirectoryInode _dirInode;

    public Directory(Context context, Inode inode, MetadataRef inodeRef)
        : base(context, inode, inodeRef)
    {
        _dirInode = inode as IDirectoryInode;
        if (_dirInode == null)
        {
            throw new ArgumentException("Inode is not a directory", nameof(inode));
        }
    }

    FastDictionary<DirectoryEntry> _allEntries;

    public IReadOnlyDictionary<string, DirectoryEntry> AllEntries
    {
        get
        {
            if (_allEntries is null)
            {
                _allEntries = new(StringComparer.Ordinal, entry => entry.FileName);

                var reader = Context.DirectoryReader;
                reader.SetPosition(_dirInode.StartBlock, _dirInode.Offset);

                // For some reason, always 3 greater than actual..
                while (reader.DistanceFrom(_dirInode.StartBlock, _dirInode.Offset) < _dirInode.FileSize - 3)
                {
                    var header = DirectoryHeader.ReadFrom(reader);

                    for (var i = 0; i < header.Count + 1; ++i)
                    {
                        var record = DirectoryRecord.ReadFrom(reader);
                        _allEntries.Add(new DirectoryEntry(header, record));
                    }
                }
            }

            return _allEntries;
        }
    }

    public DirectoryEntry Self => null;

    public DirectoryEntry GetEntryByName(string name)
        => AllEntries.TryGetValue(name, out var entry) ? entry : null;

    public DirectoryEntry CreateNewFile(string name)
    {
        throw new NotSupportedException();
    }
}