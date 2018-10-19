# NodeProxy
链式Sock5代理
Client -> Local NP -> Link NP -> ......... -> Link NP -> Remote NP -> Server

config.json说明：

host:服务地址
port:服务端口
parent_host:父节点服务地址，填写后数据会转发父节点，留空则为最终节点
parent_port:父节点服务端口
mask_number:数据包偏移位数，就是简单加密一下
in_mask:进入数据是否加密
out_mask:输出数据是否加密
