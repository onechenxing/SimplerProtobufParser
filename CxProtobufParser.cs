//----------------------------------------------
// C# 简化版全局事件发送/接收类
// @author: ChenXing
// @email:  onechenxing@163.com
// @date:   2015/04/01
//----------------------------------------------

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Text;

/// <summary>
/// Protobuf解析类
/// 支持特性：required,optional,repeated,消息嵌套
/// 支持数据类型：int32,int64,sint32,sin64,float,string,
/// 
/// </summary>
public class CxProtobufParser
{
    /// <summary>
    /// 调试输出模式
    /// </summary>
    private bool _debug = true;

    private static CxProtobufParser _instance;
    /// <summary>
    /// 单例
    /// </summary>
    public static CxProtobufParser instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = new CxProtobufParser();
            }
            return _instance;
        }
    }

    /// <summary>
    /// 调试输出
    /// </summary>
    /// <param name="info"></param>
    public void Log(object info)
    {
        if (_debug)
        {
            Debug.Log(info);
        }
    }

    /// <summary>
    /// 解析主函数
    /// </summary>
    /// <param name="data">字节流数据</param>
    /// <returns></returns>
    public CxProtobufData Parser(byte[] data)
    {
        CxProtobufData pbData = new CxProtobufData();
        Log("-----------------开始解析-------------------");
        //读取二进制数据
        for (int i = 0; i < data.Length; i++)
        {
            Log(Convert.ToString(data[i], 10).PadLeft(3, ' ') + " : " + Convert.ToString(data[i], 2).PadLeft(8, '0'));
        }

        //构建字节流
        BufferedStream bs = new BufferedStream(new MemoryStream());
        bs.Write(data, 0, data.Length);
        bs.Position = 0;

        int times = 0;
        //解析
        while (bs.Position < bs.Length)
        {
            ParserOne(bs, pbData);
            times++;
            if(times > 100)
            {
                Log("!!!!!!!!解析超过100条数据，查看是否数据错误!!!!!!!!!");
                break;
            }
        }

        Log(string.Format("canRead:{0},position:{1},length:{2}", bs.CanRead, bs.Position, bs.Length));
        Log("-----------------完成解析-------------------");
        return pbData;
    }

    /// <summary>
    /// 解析一个数据
    /// </summary>
    /// <param name="bs">字节流</param>
    /// <param name="table">存入table</param>
    private void ParserOne(BufferedStream bs, CxProtobufData pbData)
    {
        object obj = null;
        //解析key
        int key = (int)GetVarint(bs);
        int wireType = GetVarint_WireType(key);
        int filedNum = GetVarint_FieldNum(key);
        Log(String.Format("key:{0} wireType:{1} filedNum:{2}", key, wireType, filedNum));
        /*-------------------------------------------------------------------------*
            消息类型对应表
            0	Varint	int32, int64, uint32, uint64, sint32, sint64, bool, enum
            1	64-bit	fixed64, sfixed64, double
            2	Length-delimited	string, bytes, embedded messages, packed repeated fields
            3	Start group	groups (deprecated)
            4	End group	groups (deprecated)
            5	32-bit	fixed32, sfixed32, float
         *-------------------------------------------------------------------------*/
        //解析内容
        switch (wireType)
        {
            case 0://Varint:	int32, int64, uint32, uint64, sint32, sint64, bool, enum
                obj = GetVarint(bs);
                Log("》》Varint:" + obj);
                break;
            case 1://64-bit:	fixed64, sfixed64, double

                break;
            case 2://Length-delimited:	string, bytes, embedded messages, packed repeated fields
                obj = GetBytes(bs);
                Log("》》Length-delimited:" + obj);
                break;
            case 3://Start group	groups (deprecated)

                break;
            case 4://End group	groups (deprecated)

                break;
            case 5://32-bit	fixed32, sfixed32, float
                obj = Get32Bit(bs);
                Log("》》32-bit:" + obj);
                break;
        }
        pbData.Add(filedNum, obj);
    }

    /// <summary>
    /// 从字节流中获得一个变长整数
    /// 每个字节第一位为1则表示后面字节还有数据
    /// </summary>
    /// <param name="bs"></param>
    /// <returns></returns>
    private long GetVarint(BufferedStream bs)
    {
        string str = "";
        while (bs.Position < bs.Length)
        {
            int b = bs.ReadByte();
            str = Convert.ToString(b & 0x7F, 2).PadLeft(7, '0') + str;//Little Endian
            if (b >> 7 == 0)
            {                
                break;
            }
        }
        return Convert.ToInt64(str,2);
    }


    /// <summary>
    /// 获取key中存储的数据类型
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private int GetVarint_WireType(int key)
    {
        return (key & 0x7);
    }

    /// <summary>
    /// 获取key中存储的字段编号(proto中等号后面的数字)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private int GetVarint_FieldNum(int key)
    {
        return (key >> 3);
    } 

    /// <summary>
    /// 解析字节流
    /// </summary>
    /// <param name="bs"></param>
    /// <returns></returns>
    private byte[] GetBytes(BufferedStream bs)
    {
        int len = Convert.ToInt32(GetVarint(bs));//长度
        byte[] bytes = new byte[len];
        bs.Read(bytes, 0, len);
        return bytes;   
    }

    /// <summary>
    /// 解析32-bit
    /// </summary>
    /// <param name="bs"></param>
    /// <returns></returns>
    private byte[] Get32Bit(BufferedStream bs)
    {
        byte[] bytes = new byte[4];
        bs.Read(bytes, 0, 4);
        return bytes;
    } 
}

/// <summary>
/// protobuf数据类
/// </summary>
public class CxProtobufData
{
    private Hashtable _table = new Hashtable();

    /// <summary>
    /// 添加数据
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <param name="data"></param>
    public void Add(int fieldIndex,object data)
    {
        //数组repeated处理
        if(_table.ContainsKey(fieldIndex))
        {
            List<CxProtobufData> list;
            if(_table[fieldIndex] is List<CxProtobufData>)
            {
                list = _table[fieldIndex] as List<CxProtobufData>;
            }
            else
            {
                list = new List<CxProtobufData>();
                CxProtobufData lastData = new CxProtobufData();
                lastData.Add(1, _table[fieldIndex]);
                list.Add(lastData);
                _table[fieldIndex] = list;
            }

            CxProtobufData newData = new CxProtobufData();
            newData.Add(1, data);
            list.Add(newData);
        }
        else
        {
            _table.Add(fieldIndex, data);
        }        
    }

    /// <summary>
    /// 是否有数据
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <returns></returns>
    public bool HaveField(int fieldIndex)
    {
        return _table.ContainsKey(fieldIndex);
    }

    /// <summary>
    /// 获得原始数据
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <returns></returns>
    public object GetValue(int fieldIndex)
    {
        return _table[fieldIndex];
    }

    /// <summary>
    /// 获得一个int32数据
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <returns></returns>
    public int GetInt32(int fieldIndex)
    {
        return Convert.ToInt32(_table[fieldIndex]);
    }

    /// <summary>
    /// 获得一个int64数据
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <returns></returns>
    public long GetInt64(int fieldIndex)
    {
        return Convert.ToInt64(_table[fieldIndex]);
    }

    /// <summary>
    /// 获得一个带符号优化的int32数据
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <returns></returns>
    public int GetSInt32(int fieldIndex)
    {
        int value = Convert.ToInt32(_table[fieldIndex]);
        return PbMath.Zag(value);
    }

    /// <summary>
    /// 获得一个带符号优化的int64数据
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <returns></returns>
    public long GetSInt64(int fieldIndex)
    {
        long value = Convert.ToInt64(_table[fieldIndex]);
        return PbMath.Zag(value);
    }

    /// <summary>
    /// 获得一个bool数据
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <returns></returns>
    public bool GetBool(int fieldIndex)
    {
        int value = Convert.ToInt32(_table[fieldIndex]);
        return value == 0 ? false : true;
    }

    /// <summary>
    /// 获得一个float数据
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <returns></returns>
    public float GetFloat(int fieldIndex)
    {
        return BitConverter.ToSingle((byte[])_table[fieldIndex], 0);
    }

    /// <summary>
    /// 获得一个string数据
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <returns></returns>
    public string GetString(int fieldIndex)
    {
        return Encoding.UTF8.GetString((byte[])_table[fieldIndex]);
    }

    /// <summary>
    /// 获得repeated标记数组
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <returns></returns>
    public List<CxProtobufData> GetRepeated(int fieldIndex)
    {
        List<CxProtobufData> list = new List<CxProtobufData>();
        if (HaveField(fieldIndex))
        {
            //如果只有一个数据不会构建list
            if (_table[fieldIndex] is List<CxProtobufData>)
            {
                list = _table[fieldIndex] as List<CxProtobufData>;
            }
            else
            {
                list = new List<CxProtobufData>();
                CxProtobufData lastData = new CxProtobufData();
                lastData.Add(1, _table[fieldIndex]);
                list.Add(lastData);
                _table[fieldIndex] = list;
            }
        }
        return list;
    }

    /// <summary>
    /// 获得一个子消息数据
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <returns></returns>
    public CxProtobufData GetSubMessage(int fieldIndex)
    {
        return CxProtobufParser.instance.Parser((byte[])_table[fieldIndex]);
    }
}

/// <summary>
/// protobuf发送类
/// </summary>
public class CxProtobufSender
{
    /// <summary>
    /// 流
    /// </summary>
    private BufferedStream _bs = new BufferedStream(new MemoryStream());

    /// <summary>
    /// 获取最终组合好的数据
    /// </summary>
    /// <returns></returns>
    public byte[] GetData()
    {
        byte[] b = new byte[_bs.Length];
        _bs.Position = 0;
        _bs.Read(b,0,Convert.ToInt32(_bs.Length));
        return b;
    }

    /// <summary>
    /// 写入一个int
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <param name="value"></param>
    public void WriteInt32(int fieldIndex,int value)
    {
        WriteFieldNumAndWireType(fieldIndex, 0);
        WriteVarint(Convert.ToInt64(value));
    }

    /// <summary>
    /// 写入一个long
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <param name="value"></param>
    public void WriteInt64(int fieldIndex, long value)
    {
        WriteFieldNumAndWireType(fieldIndex, 0);
        WriteVarint(value);
    }

    /// <summary>
    /// 写入一个带符号优化的int
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <param name="value"></param>
    public void WriteSInt32(int fieldIndex, int value)
    {
        WriteFieldNumAndWireType(fieldIndex, 0);
        value = PbMath.Zig(value);
        WriteVarint(Convert.ToInt64(value));
    }

    /// <summary>
    /// 写入一个带符号优化的long
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <param name="value"></param>
    public void WriteSInt64(int fieldIndex, long value)
    {
        WriteFieldNumAndWireType(fieldIndex, 0);
        value = PbMath.Zig(value);
        WriteVarint(value);
    }

    /// <summary>
    /// 写入一个bool
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <param name="value"></param>
    public void WriteBool(int fieldIndex, bool value)
    {
        WriteFieldNumAndWireType(fieldIndex, 0);
        WriteVarint(value ? 1 : 0);
    }

    /// <summary>
    /// 写入一个float
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <param name="value"></param>
    public void WriteFloat(int fieldIndex, float value)
    {
        WriteFieldNumAndWireType(fieldIndex, 5);
        _bs.Write(BitConverter.GetBytes(value), 0, 4);
    }

    /// <summary>
    /// 写入一个string
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <param name="value"></param>
    public void WriteString(int fieldIndex, string value)
    {
        WriteFieldNumAndWireType(fieldIndex, 2);
        WriteVarint(value.Length);
        _bs.Write(Encoding.UTF8.GetBytes(value),0,Encoding.UTF8.GetByteCount(value));
    }

    /// <summary>
    /// 写入一个子消息数据
    /// </summary>
    /// <param name="fieldIndex"></param>
    /// <param name="msg"></param>

    public void WriteSubMessage(int fieldIndex, CxProtobufSender value)
    {
        WriteFieldNumAndWireType(fieldIndex, 2);
        byte[] bytes = value.GetData();
        WriteVarint(bytes.Length);
        _bs.Write(bytes,0,bytes.Length);
    }

    /// <summary>
    /// 写入一个变长整数
    /// 每个字节第一位为1则表示后面字节还有数据
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private void WriteVarint(long value)
    {
        string str = Convert.ToString(value, 2);
        int i = str.Length - 7;
        for (; i >= 0; i -= 7)//Little Endian
        {
            string byteStr = str.Substring(i, 7);
            if(i == 0)
            {
                byteStr = "0" + byteStr;
            }
            else
            {
                byteStr = "1" + byteStr;
            }
            _bs.WriteByte(Convert.ToByte(byteStr, 2));
        }
        i += 7;
        if(i > 0)
        {
            string byteStr = str.Substring(0, i);
            _bs.WriteByte(Convert.ToByte(byteStr, 2));
        }
    }

    /// <summary>
    /// 写入字段编号和数据类型
    /// </summary>
    /// <param name="fieldNum"></param>
    /// <param name="wireType"></param>
    private void WriteFieldNumAndWireType(int fieldNum,int wireType)
    {
        int value = (fieldNum << 3) | wireType;
        WriteVarint(Convert.ToInt64(value));
    }    
}

/// <summary>
/// 算法辅助类
/// </summary>
internal class PbMath
{
    /// <summary>
    /// ZigZag编码
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    internal static int Zig(int value)
    {
        return ((value << 1) ^ (value >> 31));
    }

    /// <summary>
    /// ZigZag编码
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    internal static long Zig(long value)
    {
        return ((value << 1) ^ (value >> 63));
    }

    /// <summary>
    /// ZigZag解码
    /// </summary>
    /// <param name="ziggedValue"></param>
    /// <returns></returns>
    internal static int Zag(int value)
    {
        return (-(value & 0x01)) ^ ((value >> 1) & ~(((int)1) << 31));
    }

    /// <summary>
    /// ZigZag解码
    /// </summary>
    /// <param name="ziggedValue"></param>
    /// <returns></returns>
    internal static long Zag(long value)
    {
        return (-(value & 0x01L)) ^ ((value >> 1) & ~(((long)1) << 63));
    }
}
