﻿using System;
using System.Collections.Generic;
using Core.WindJson;
using Core.Serializer;
using Core;
using System.IO;

namespace Model
{
    public class SerializerPacker : IMessagePacker
    {
        public byte[] SerializeTo(object obj)
        {
            return ProtobufHelper.ToBytes(obj);
        }

        public void SerializeTo(object obj, MemoryStream stream)
        {
            ProtobufHelper.ToStream(obj, stream);
        }

        public object DeserializeFrom(Type type, byte[] bytes, int index, int count)
        {
            return ProtobufHelper.FromBytes(type, bytes, index, count);
        }

        public object DeserializeFrom(object instance, byte[] bytes, int index, int count)
        {
            return ProtobufHelper.FromBytes(instance, bytes, index, count);
        }

        public object DeserializeFrom(Type type, MemoryStream stream)
        {
            return ProtobufHelper.FromStream(type, stream);
        }

        public object DeserializeFrom(object instance, MemoryStream stream)
        {
            return ProtobufHelper.FromStream(instance, stream);
        }
    }
}
