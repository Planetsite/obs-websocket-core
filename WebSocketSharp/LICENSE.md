The MIT License

Copyright (c) 2012-2015 sta.blockhead

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

--------------------

This code is derived from WebHeaderCollection.cs (System.Net) of Mono
(http://www.mono-project.com).

The MIT License

Copyright (c) 2003 Ximian, Inc. (http://www.ximian.com)
Copyright (c) 2007 Novell, Inc. (http://www.novell.com)
Copyright (c) 2012-2020 sta.blockhead

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Contributors:
	HttpRequest - David Burhans
	Ext - Liryna <liryna.stark@gmail.com>, Nikola Kovacevic <nikolak@outlook.com>, Chris Swiedler
	ErrorEventArgs - Frank Razenberg <frank@zzattack.org>
	WebSocket - Frank Razenberg <frank@zzattack.org>, David Wood <dpwood@gmail.com>, Liryna <liryna.stark@gmail.com>
	WebSocketServiceHost - Juan Manuel Lallana <juan.manuel.lallana@gmail.com>
	HttpServer - Juan Manuel Lallana <juan.manuel.lallana@gmail.com>, Liryna <liryna.stark@gmail.com>, Rohan Singh <rohan-singh@hotmail.com>
	WebSocketServer - Juan Manuel Lallana <juan.manuel.lallana@gmail.com>, Jonas Hovgaard <j@jhovgaard.dk>, Liryna <liryna.stark@gmail.com>, Rohan Singh <rohan-singh@hotmail.com>
	TcpListenerWebSocketContext - Liryna <liryna.stark@gmail.com>
	WebHeaderCollection - Lawrence Pit <loz@cable.a2000.nl>, Gonzalo Paniagua Javier <gonzalo@ximian.com>, Miguel de Icaza <miguel@novell.com>
	ServerSslConfiguration - Liryna <liryna.stark@gmail.com>
	ResponseStream - Gonzalo Paniagua Javier <gonzalo@novell.com>
	RequestStream - Gonzalo Paniagua Javier <gonzalo@novell.com>
	ReadBufferState - Gonzalo Paniagua Javier <gonzalo@novell.com>
	QueryStringCollection - Patrik Torstensson <Patrik.Torstensson@labs2.com>, Wictor Wilén (decode/encode functions) <wictor@ibizkit.se>, Tim Coleman <tim@timcoleman.com>, Gonzalo Paniagua Javier <gonzalo@ximian.com>
	LineState - Gonzalo Paniagua Javier <gonzalo@novell.com>
	InputState - Gonzalo Paniagua Javier <gonzalo@novell.com>
	InputChunkState - Gonzalo Paniagua Javier <gonzalo@novell.com>
	HttpVersion - Lawrence Pit <loz@cable.a2000.nl>
	HttpStreamAsyncResult - Gonzalo Paniagua Javier <gonzalo@novell.com>
	HttpResponseHeader - Gonzalo Paniagua Javier <gonzalo@novell.com>
	HttpRequestHeader - Gonzalo Paniagua Javier <gonzalo@novell.com>
	HttpListenerResponse - Gonzalo Paniagua Javier <gonzalo@novell.com>, Nicholas Devenish
	HttpListenerRequest - Gonzalo Paniagua Javier <gonzalo@novell.com>
	HttpListenerPrefixCollection - Gonzalo Paniagua Javier <gonzalo@novell.com>
	HttpListenerException - Gonzalo Paniagua Javier <gonzalo@novell.com>
	HttpListenerPrefix - Gonzalo Paniagua Javier <gonzalo@novell.com>, Oleg Mihailik <mihailik@gmail.com>
	HttpUtility - Patrik Torstensson <Patrik.Torstensson@labs2.com>, Wictor Wilén (decode/encode functions) <wictor@ibizkit.se>, Tim Coleman <tim@timcoleman.com>, Gonzalo Paniagua Javier <gonzalo@ximian.com>
	HttpListenerContext - Gonzalo Paniagua Javier <gonzalo@novell.com>, Oleg Mihailik <mihailik@gmail.com>
	HttpListenerAsyncResult - Gonzalo Paniagua Javier <gonzalo@novell.com>, Oleg Mihailik <mihailik@gmail.com>, Nicholas Devenish
	HttpListener - Gonzalo Paniagua Javier <gonzalo@novell.com>, Liryna <liryna.stark@gmail.com>
	AuthenticationSchemes - Atsushi Enomoto <atsushi@ximian.com>
	HttpConnection - Gonzalo Paniagua Javier <gonzalo@novell.com>, Liryna <liryna.stark@gmail.com>, Rohan Singh <rohan-singh@hotmail.com>
	HttpBasicIdentity - Gonzalo Paniagua Javier <gonzalo@novell.com>
	Chunk - Gonzalo Paniagua Javier <gonzalo@novell.com>
	ChunkedRequestStream - Gonzalo Paniagua Javier <gonzalo@novell.com>
	ChunkStream - Gonzalo Paniagua Javier <gonzalo@novell.com>
	ClientSslConfiguration - Liryna <liryna.stark@gmail.com>
	Cookie - Lawrence Pit <loz@cable.a2000.nl>, Gonzalo Paniagua Javier <gonzalo@ximian.com>, Daniel Nauck <dna@mono-project.de>, Sebastien Pouliot <sebastien@ximian.com>
	CookieCollection - Lawrence Pit <loz@cable.a2000.nl>, Gonzalo Paniagua Javier <gonzalo@ximian.com>, Sebastien Pouliot <sebastien@ximian.com>
	CookieException - Lawrence Pit <loz@cable.a2000.nl>
	EndPointListener - Gonzalo Paniagua Javier <gonzalo@novell.com>, Liryna <liryna.stark@gmail.com>, Nicholas Devenish
	EndPointManager - Gonzalo Paniagua Javier <gonzalo@ximian.com>, Liryna <liryna.stark@gmail.com>
