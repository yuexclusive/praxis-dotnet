﻿using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Reflection;

namespace LY.Common.LYMQ
{
    public class LYMQ : IMQ
    {
        /// <summary>
        /// 开始服务
        /// </summary>
        public void StartServer()
        {
            var handlerTypes = Assembly.GetEntryAssembly().ExportedTypes.Where(x => (typeof(IMQHandler)).IsAssignableFrom(x));
            using (NetMQSocket serverSocket = new ResponseSocket())
            {
                serverSocket.Bind(Address);
                while (true)
                {
                    string message = serverSocket.ReceiveFrameString();
                    object returnObj= null;
                    try
                    {
                        var transfer = JsonConvert.DeserializeObject<MQSend>(message);

                        var handlerType = handlerTypes.FirstOrDefault(x => x.Name == transfer.HandlerTypeName);
                        if (handlerType == null)
                        {
                            throw new Exception($"无法找到处理类{transfer.HandlerTypeName}");
                        }

                        var method = handlerType.GetMethod(transfer.HandlerMethodName);
                        if (method == null)
                        {
                            throw new Exception($"处理类{transfer.HandlerTypeName}缺少方法{transfer.HandlerMethodName}");
                        }

                        var parameters = new List<object> { };
                        if (!string.IsNullOrEmpty(transfer.ParameterContent) && !string.IsNullOrEmpty(transfer.ParameterAssemblyQualifiedName))
                        {
                            var parameterType = Type.GetType(transfer.ParameterAssemblyQualifiedName);
                            if (parameterType != null)
                            {
                                var parameterObj = JsonConvert.DeserializeObject(transfer.ParameterContent, parameterType);
                                if (parameterObj != null)
                                {
                                    parameters.Add(parameterObj);
                                }
                            }
                        }

                        returnObj = method.Invoke(Activator.CreateInstance(handlerType), parameters.ToArray());

                        if (returnObj != null)
                        {
                            returnObj.GetType().GetProperty("Status").SetValue(returnObj, MQResultStatus.Sucess);
                        }
                        else
                        {
                            returnObj = new MQResult()
                            {
                                Status = MQResultStatus.Sucess
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        returnObj = new MQResult() { Status = MQResultStatus.Fail, Msg = ex.Message };
                    }
                    finally
                    {
                        serverSocket.SendFrame(JsonConvert.SerializeObject(returnObj));
                        LogUtil.Logger<LYMQ>().LogError(JsonConvert.SerializeObject(returnObj));
                    }
                }
            }
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="handlerTypeName">类名</param>
        /// <param name="handlerMethodName">方法名</param>
        /// <param name="parameterObj">参数对象</param>
        public T Send<T>(string handlerTypeName, string handlerMethodName, object parameterObj = null) where T : MQResult
        {
            using (NetMQSocket clientSocket = new RequestSocket())
            {
                clientSocket.Connect(Address);

                string parameterAssemblyQualifiedName = string.Empty;
                string parameterContent = string.Empty;
                if (parameterObj != null)
                {
                    parameterAssemblyQualifiedName = parameterObj.GetType().AssemblyQualifiedName;
                    parameterContent = JsonConvert.SerializeObject(parameterObj);
                }
                MQSend transfer = new MQSend()
                {
                    HandlerTypeName = handlerTypeName,
                    HandlerMethodName = handlerMethodName,
                    ParameterAssemblyQualifiedName = parameterAssemblyQualifiedName,
                    ParameterContent = parameterContent
                };
                string strContent = JsonConvert.SerializeObject(transfer);
                clientSocket.SendFrame(strContent);

                T result = JsonConvert.DeserializeObject<T>(clientSocket.ReceiveFrameString());
                return result;
            }
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="handlerTypeName">类名</param>
        /// <param name="handlerMethodName">方法名</param>
        /// <param name="parameterObj">参数对象</param>
        public MQResult Send(string handlerTypeName, string handlerMethodName, object parameterObj = null)
        {
            return Send<MQResult>(handlerTypeName, handlerMethodName, parameterObj);
        }

        /// <summary>
        /// MQ服务端地址
        /// </summary>
        private string Address
        {
            get
            {
                return ConfigUtil.ConfigurationRoot["LYMQ:Address"];
            }
        }
    }
}
