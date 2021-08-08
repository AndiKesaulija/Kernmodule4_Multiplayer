using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;
using System.Reflection;

namespace ChatClientExample
{
    public class CallOnFunctionMessage : MessageHeader
    {

        public override NetworkMessageType Type { get { return NetworkMessageType.CALL_ON_FUNCTION; } }

        public Client target;
        public string methodName;
        public object[] data;

        public MethodInfo mInfo;
        public ParameterInfo[] parameters;

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);

            writer.WriteFixedString128(methodName);
            //mInfo = target.GetType().GetMethod(methodName);
            //if (mInfo == null)
            //{
            //    throw new System.ArgumentException($"Object {target.GetType()} does not conaint {methodName}");
            //}

            //parameters = mInfo.GetParameters();

            //for (int i = 0; i < parameters.Length; i++)
            //{
            //    if (parameters[i].ParameterType == typeof(string))
            //    {
            //        writer.WriteFixedString128((string)data[i]);
            //    }
            //    else if (parameters[i].ParameterType == typeof(float))
            //    {
            //        writer.WriteFloat((float)data[i]);
            //    }
            //    else if (parameters[i].ParameterType == typeof(int))
            //    {
            //        writer.WriteInt((int)data[i]);
            //    }
            //    else if (parameters[i].ParameterType == typeof(Vector3))
            //    {
            //        Vector3 vec = (Vector3)data[i];
            //        writer.WriteFloat(vec.x);
            //        writer.WriteFloat(vec.y);
            //        writer.WriteFloat(vec.z);
            //    }
            //    else if (parameters[i].ParameterType == typeof(IEnumerable))
            //    {
            //        writer.WriteInt((int)data[i]);
            //    }

            //    else
            //    {
            //        throw new System.ArgumentException($"Unhandled RPC type: {parameters[i].ParameterType.ToString()}");
            //    }
            //}

        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);

            methodName = reader.ReadFixedString128().ToString();

            target = Object.FindObjectOfType<Client>();
            mInfo = target.GetType().GetMethod(methodName);
            if (mInfo == null)
            {
                throw new System.ArgumentException($"Object of Type{target.GetType()} does not conaint method{methodName}");
            }

            //parameters = mInfo.GetParameters();

            //data = new object[parameters.Length];
            //data[0] = Object.FindObjectOfType<Server>();


            ////Skip parameters[0] == Server
            //for (int i = 1; i < parameters.Length; i++)
            //{
            //    if (parameters[i].ParameterType == typeof(string))
            //    {
            //        data[i] = reader.ReadFixedString128().ToString();
            //    }
            //    else if (parameters[i].ParameterType == typeof(float))
            //    {
            //        data[i] = reader.ReadFloat();
            //    }
            //    else if (parameters[i].ParameterType == typeof(int))
            //    {
            //        data[i] = reader.ReadInt();
            //    }
            //    else if (parameters[i].ParameterType == typeof(Vector3))
            //    {
            //        data[i] = new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
            //    }
            //    else if (parameters[i].ParameterType == typeof(IEnumerable))
            //    {
            //        data[i] = reader.ReadInt();
            //    }
            //    else
            //    {
            //        throw new System.ArgumentException($"Unhandled RPC type: {parameters[i].ParameterType.ToString()}");
            //    }
            //}


        }
    }
}

