using System;
using System.Collections.Generic;

namespace NodeProxy.Socks5
{
    /// <summary>
    /// Socks5的登录方式
    /// </summary>
    public enum Socks5AuthType
    {
        NO_AUTHENTICATION_REQUIRED = 0x00,
        GSSAPI = 0x01,
        USERNAME_PASSWORD = 0x02,
        // X'03' to X'7F' IANA ASSIGNED
        // X'80' to X'FE' RESERVED FOR PRIVATE METHODS
        NO_ACCEPTABLE_METHODS = 0xFF,
    }

    /// <summary>
    /// Socks5命令
    /// </summary>
    public enum Socks5CMD
    {
        CONNECT = 0x01,
        BIND = 0x02,
        UDP_ASSOCIATE = 0x03,
    }

    /// <summary>
    /// 地址类型
    /// </summary>
    public enum Socks5ATYP
    {
        IPV4 = 0x01,
        DomainName = 0x03,
        IPV6 = 0x04,
    }

    //o X'00' succeeded
    //o X'01' general SOCKS server failure
    //o X'02' connection not allowed by ruleset
    //o X'03' Network unreachable
    //o X'04' Host unreachable
    //o X'05' Connection refused
    //o X'06' TTL expired
    //o X'07' Command not supported
    //o X'08' Address type not supported
    //o X'09' to X'FF' unassigned
    /// <summary>
    /// 请求回复
    /// </summary>
    public enum Socks5Reply
    {
        Succeeded = 0x00,
        SeneralSOCKSServerFailure = 0x01,
        ConnectionNotAllowedByRuleset = 0x02,
        NetworkUnreachable = 0x03,
        HostUnreachable = 0x04,
        ConnectionRefused = 0x05,
        TTLExpired = 0x06,
        CommandNotSupported = 0x07,
        AddressTypeNotSupported = 0x08,
        // X'09' to X'FF' unassigned
    }

    public enum Socks5NodeType
    {
        Client = 0,
        Server = 1,
        Node = 2,
    }
}
