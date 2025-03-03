//
// Copyright (c) 2016, Bianco Veigel
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
using DiscUtils.Streams;

namespace DiscUtils.Xfs;

internal class ShortformDirectoryEntry : IByteArraySerializable, IDirectoryEntry
{
    private readonly bool _useShortInode;
    private readonly bool _ftype;

    public ShortformDirectoryEntry(bool useShortInode, Context context)
    {
        _useShortInode = useShortInode;
        _ftype = context.SuperBlock.HasFType;
    }

    public byte NameLength { get; private set; }

    public ushort Offset { get; private set; }

    public byte[] Name { get; private set; }

    public ulong Inode { get; private set; }

    public DirectoryFType FType { get; private set; }

    public int Size => 0x3 + NameLength + (_useShortInode ? 4 : 8) + (_ftype ? 1 : 0);

    public int ReadFrom(ReadOnlySpan<byte> buffer)
    {
        NameLength = buffer[0];
        Offset = EndianUtilities.ToUInt16BigEndian(buffer.Slice(0x1));
        Name = EndianUtilities.ToByteArray(buffer.Slice(0x3, NameLength));
        var offset = 0x3 + NameLength;
        if (_ftype)
        {
            FType = (DirectoryFType)buffer[offset];
            offset++;
        }

        if (_useShortInode)
        {
            Inode = EndianUtilities.ToUInt32BigEndian(buffer.Slice(offset));
        }
        else
        {
            Inode = EndianUtilities.ToUInt64BigEndian(buffer.Slice(offset));
        }

        return Size;
    }

    void IByteArraySerializable.WriteTo(Span<byte> buffer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var latin1Encoding = EncodingUtilities.GetLatin1Encoding();

        return $"{Inode}: {latin1Encoding.GetString(Name, 0, NameLength)}";
    }
}
