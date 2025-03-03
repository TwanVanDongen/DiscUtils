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
using System.IO;
using System.Text;
using DiscUtils.Streams;
using DiscUtils.Streams.Compatibility;

namespace DiscUtils.Ntfs;

internal class AttributeListRecord : IDiagnosticTraceable, IByteArraySerializable, IComparable<AttributeListRecord>
{
    public ushort AttributeId;
    public FileRecordReference BaseFileReference;
    public string Name;
    public byte NameLength;
    public byte NameOffset;
    public ushort RecordLength;
    public ulong StartVcn;
    public AttributeType Type;

    public int Size => MathUtilities.RoundUp(0x20 + (string.IsNullOrEmpty(Name) ? 0 : Encoding.Unicode.GetByteCount(Name)),
                8);

    public int ReadFrom(ReadOnlySpan<byte> data)
    {
        Type = (AttributeType)EndianUtilities.ToUInt32LittleEndian(data);
        RecordLength = EndianUtilities.ToUInt16LittleEndian(data.Slice(0x04));
        NameLength = data[0x06];
        NameOffset = data[0x07];
        StartVcn = EndianUtilities.ToUInt64LittleEndian(data.Slice(0x08));
        BaseFileReference = new FileRecordReference(EndianUtilities.ToUInt64LittleEndian(data.Slice(0x10)));
        AttributeId = EndianUtilities.ToUInt16LittleEndian(data.Slice(0x18));

        if (NameLength > 0)
        {
            Name = Encoding.Unicode.GetString(data.Slice(NameOffset, NameLength * 2));
        }
        else
        {
            Name = null;
        }

        if (RecordLength < 0x18)
        {
            throw new InvalidDataException("Malformed AttributeList record");
        }

        return RecordLength;
    }

    public void WriteTo(Span<byte> buffer)
    {
        NameOffset = 0x20;
        if (string.IsNullOrEmpty(Name))
        {
            NameLength = 0;
        }
        else
        {
            NameLength = (byte)(Encoding.Unicode.GetBytes(Name.AsSpan(), buffer.Slice(NameOffset)) / 2);
        }

        RecordLength = (ushort)MathUtilities.RoundUp(NameOffset + NameLength * 2, 8);

        EndianUtilities.WriteBytesLittleEndian((uint)Type, buffer);
        EndianUtilities.WriteBytesLittleEndian(RecordLength, buffer.Slice(0x04));
        buffer[0x06] = NameLength;
        buffer[0x07] = NameOffset;
        EndianUtilities.WriteBytesLittleEndian(StartVcn, buffer.Slice(0x08));
        EndianUtilities.WriteBytesLittleEndian(BaseFileReference.Value, buffer.Slice(0x10));
        EndianUtilities.WriteBytesLittleEndian(AttributeId, buffer.Slice(0x18));
    }

    public int CompareTo(AttributeListRecord other)
    {
        var val = (int)Type - (int)other.Type;
        if (val != 0)
        {
            return val;
        }

        val = string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        if (val != 0)
        {
            return val;
        }

        return (int)StartVcn - (int)other.StartVcn;
    }

    public void Dump(TextWriter writer, string indent)
    {
        writer.WriteLine($"{indent}ATTRIBUTE LIST RECORD");
        writer.WriteLine($"{indent}                 Type: {Type}");
        writer.WriteLine($"{indent}        Record Length: {RecordLength}");
        writer.WriteLine($"{indent}                 Name: {Name}");
        writer.WriteLine($"{indent}            Start VCN: {StartVcn}");
        writer.WriteLine($"{indent}  Base File Reference: {BaseFileReference}");
        writer.WriteLine($"{indent}         Attribute ID: {AttributeId}");
    }

    public static AttributeListRecord FromAttribute(AttributeRecord attr, FileRecordReference mftRecord)
    {
        var newRecord = new AttributeListRecord
        {
            Type = attr.AttributeType,
            Name = attr.Name,
            StartVcn = 0,
            BaseFileReference = mftRecord,
            AttributeId = attr.AttributeId
        };

        if (attr.IsNonResident)
        {
            newRecord.StartVcn = (ulong)((NonResidentAttributeRecord)attr).StartVcn;
        }

        return newRecord;
    }
}