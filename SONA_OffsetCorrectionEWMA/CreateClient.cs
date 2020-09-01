using Modbus.Device;
using Modbus.Message;
using Modbus.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SONA_OffsetCorrectionEWMA
{
    class CreateClient
    {
        private string _ipAddress;
        private string _machineId;
        private int _portNo;
        private string _interfaceId;
        private string _protocol;
        private string MName;
        MachineInfoDTO _machineDTO = null;


        private static string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        TcpClient tcpClient = null;
        ushort commNo = 100;
        private readonly ushort HoldingRegisterForCommunictaion = 600;

        #region Holding Registers For Offset Correction Process
        private readonly ushort HoldingRegForReadDataFlag = 605;//606;
        private readonly ushort HoldingRegForPartID = 608;//609;
        private readonly ushort HoldingRegForSerialNo = 606;//607;
        private readonly ushort HoldingRegForACK = 607;//608;
        private readonly ushort HoldingRegForOperationID = 609;//610;
        private readonly ushort HoldingRegForDimension = 610;//611;
        private readonly ushort HoldingRegForMean = 611;//612;
        private readonly ushort HoldingRegForLambda = 613;//614;
        private readonly ushort HoldingRegForL = 615;//616;
        private readonly ushort HoldingRegForSigma = 617;//618;
        private readonly ushort HoldingRegForMeasuredVal = 619;//620;
        private readonly ushort HoldingRegForEWMA = 621;//622;
        private readonly ushort HoldingRegForLCL = 623;//624;
        private readonly ushort HoldingRegForUCL = 625;//626;
        private readonly ushort HoldingRegForCorrectionVal = 627;//628;
        private readonly ushort HoldingRegForMaxCorrectionStep = 629;//630;
        private readonly ushort HoldingRegForMeasuredDate = 631;//632;
        private readonly ushort HoldingRegForMeasuredTime = 633;//634;
        private readonly ushort HoldingRegisterForIterationCount = 600;//601;
        private readonly ushort HoldingRegisterForResetBit = 602;//603;
        #endregion
        public CreateClient(MachineInfoDTO machine)
        {
            this._machineDTO = machine;
            this._ipAddress = machine.IpAddress;
            this._portNo = machine.PortNo;
            this._machineId = machine.MachineId;
            this._interfaceId = machine.InterfaceId;
            this._protocol = string.IsNullOrEmpty(machine.DataCollectionProtocol) ? "RAW" : machine.DataCollectionProtocol;
            this.MName = machine.MachineId;
        }

        internal void GetClient()
        {
            while (true)
            {
                try
                {
                    #region stop_service                   
                    if (ServiceStop.stop_service == 1)
                    {
                        try
                        {
                            Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteErrorLog(ex.ToString());
                            break;
                        }
                    }
                    #endregion

                    #region "Modbus"
                    else if (_protocol.Equals("modbus", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.WriteDebugLog("Entered into MODBUS Protocol");
                        ModbusIpMaster master = default(ModbusIpMaster);
                        commNo = 100;
                        DateTime timeToUpdateDate = DateTime.MinValue;
                        while (true)
                        {
                            #region StopService
                            if (ServiceStop.stop_service == 1)
                            {
                                try
                                {
                                    if (master != null)
                                    {
                                        master.Dispose();
                                        master = null;
                                    }
                                    Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteErrorLog(ex.Message);
                                    break;
                                }
                            }
                            #endregion

                            //Why below condition?
                            try
                            {
                                master = ConnectModBus();
                                if (master != null)
                                {
                                    HandingOffsetCorrectionValues(ref master);
                                }
                                else
                                {
                                    Logger.WriteDebugLog("Disconnected from network (No ping).");
                                    Thread.Sleep(1000);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteErrorLog(ex.ToString());
                            }
                            finally
                            {
                                if (tcpClient != null && tcpClient.Connected)
                                {
                                    tcpClient.Client.Shutdown(SocketShutdown.Both);
                                    tcpClient.Close();
                                    tcpClient = null;
                                }

                                if (master != null)
                                {
                                    master.Dispose();
                                }

                                master = null;
                            }
                            Thread.Sleep(1500);
                        }
                    }
                    #endregion

                    
                }
                catch (Exception ex)
                {
                    Logger.WriteErrorLog("Exception from main while loop : " + ex.ToString());
                    Thread.Sleep(2000);
                }
            }
            Logger.WriteDebugLog("End of while loop." + Environment.NewLine + "------------------------------------------");
        }

        private void HandingOffsetCorrectionValues(ref ModbusIpMaster master)
        {
            if (master == null)
            {
                Logger.WriteDebugLog("Getting Null for ModbusIpMaster object master. Exiting From HandingOffsetCorrectionValues()");
                return;
            }

            ushort[] ReadDataFlag;
            ushort[] Output;
            int SlNo ;
            int PartID;
            int OperationID;
            int Dimension;
            int IterationCount;
            float MeanVal;
            float LambdaVal;
            float LVal;
            float SigmaVal;
            float MeasuredValue;
            float EWMAVal;
            float LCLVal;
            float UCLVal;
            float CorrectionValue;
            float MaxCorrectionStep;
            DateTime MeasuredDateTime;
            
            try
            {
                ReadDataFlag = master.ReadHoldingRegisters(HoldingRegForReadDataFlag, (ushort)1);

                if (ReadDataFlag[0] == 1)
                {
                    Logger.WriteDebugLog("Read Flag (606) is High");
                    Output = master.ReadHoldingRegisters(HoldingRegForSerialNo, (ushort)1);
                    SlNo = Output[0];
                    Output = master.ReadHoldingRegisters(HoldingRegForPartID, (ushort)1);
                    PartID = Output[0];
                    Output = master.ReadHoldingRegisters(HoldingRegForDimension, (ushort)1);
                    Dimension = Output[0];
                    Output = master.ReadHoldingRegisters(HoldingRegForOperationID, (ushort)1);
                    OperationID = Output[0];
                    Output = master.ReadHoldingRegisters(HoldingRegisterForIterationCount, (ushort)1);
                    IterationCount = Output[0];

                    Output = master.ReadHoldingRegisters(HoldingRegForMean, (ushort)2);
                    MeanVal = GetFloat(Output);
                    Output = master.ReadHoldingRegisters(HoldingRegForLambda, (ushort)2);
                    LambdaVal = GetFloat(Output);
                    Output = master.ReadHoldingRegisters(HoldingRegForL, (ushort)2);
                    LVal = GetFloat(Output);
                    Output = master.ReadHoldingRegisters(HoldingRegForSigma, (ushort)2);
                    SigmaVal = GetFloat(Output);
                    Output = master.ReadHoldingRegisters(HoldingRegForMeasuredVal, (ushort)2);
                    MeasuredValue = GetFloat(Output);
                    Output = master.ReadHoldingRegisters(HoldingRegForEWMA, (ushort)2);
                    EWMAVal = GetFloat(Output);
                    Output = master.ReadHoldingRegisters(HoldingRegForLCL, (ushort)2);
                    LCLVal = GetFloat(Output);
                    Output = master.ReadHoldingRegisters(HoldingRegForUCL, (ushort)2);
                    UCLVal = GetFloat(Output);
                    Output = master.ReadHoldingRegisters(HoldingRegForCorrectionVal, (ushort)2);
                    CorrectionValue = GetFloat(Output);
                    Output = master.ReadHoldingRegisters(HoldingRegForMaxCorrectionStep, (ushort)2);
                    MaxCorrectionStep = GetFloat(Output);

                    Output = master.ReadHoldingRegisters(HoldingRegForMeasuredDate, (ushort)2);
                    string MeasuredDate = ModbusUtility.GetUInt32(Output[1], Output[0]).ToString("00000000");
                    Output = master.ReadHoldingRegisters(HoldingRegForMeasuredTime, (ushort)2);
                    string MeasuredTime = ModbusUtility.GetUInt32(Output[1], Output[0]).ToString("000000");
                    MeasuredDateTime = DateTime.ParseExact(string.Format("{0}{1}", MeasuredDate, MeasuredTime), "yyyyMMddHHmmss", null);

                    Logger.WriteDebugLog(string.Format("Data Received : MC-{0} PartID-{1} Operation-{2} Operator-{3} Dimension-{4} Mean-{5} Lambda-{6} L-{7} Sigma-{8} Measured Value-{9} EWMA-{10} LCL-{11} UCL-{12} Correction Value-{13} Max Correction Step-{14} Measured DateTime-{15} Iteration Count-{16}", this._interfaceId, PartID, OperationID, "1", Dimension, MeanVal, LambdaVal, LVal, SigmaVal, MeasuredValue, EWMAVal, LCLVal, UCLVal, CorrectionValue, MaxCorrectionStep, MeasuredDateTime.ToString("dd-MMM-yyyy HH:mm:ss"), IterationCount));

                    DatabaseAccess.InsertDataToSPCAutoData(this._interfaceId, PartID, OperationID,"1", Dimension, MeanVal, LambdaVal, LVal, SigmaVal, MeasuredValue, EWMAVal, LCLVal, UCLVal, CorrectionValue, MaxCorrectionStep, MeasuredDateTime, IterationCount);

                    master.WriteSingleRegister(HoldingRegForACK, (ushort)SlNo);
                    master.WriteSingleRegister(HoldingRegForReadDataFlag, (ushort)2);
                    Logger.WriteDebugLog("Read Flag (606) is set to Low");
                }

                #region Communication
                //send communicationNo
                try
                {
                    master.WriteSingleRegister(HoldingRegisterForCommunictaion, commNo);
                }
                catch (Exception ex)
                {
                    if (master != null) master.Dispose();
                    Logger.WriteErrorLog(ex.ToString());
                }
                if (commNo == 100)
                {
                    commNo = 200;
                }
                else commNo = 100;

                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog("Exception In HandingOffsetCorrectionValues() : " + ex.Message);
            }            
        }

        private ModbusIpMaster ConnectModBus()
        {
            ModbusIpMaster master = null;
            int count = 0;
            Ping netMon = default(Ping);
            netMon = new Ping();
            PingReply reply = null;
            do
            {
                #region StopService
                if (ServiceStop.stop_service == 1)
                {
                    try
                    {
                        if (tcpClient != null && tcpClient.Connected)
                        {
                            tcpClient.Close();
                        }

                        if (master != null)
                        {
                            master.Dispose();
                        }

                        Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteErrorLog(ex.Message);
                        break;
                    }
                }
                #endregion

                try
                {
                    reply = netMon.Send(this._ipAddress, 4000);
                    if (reply.Status == IPStatus.Success)
                    {
                        tcpClient = new TcpClient(this._ipAddress, this._portNo);  //Port no is always 502
                        tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        Thread.Sleep(300);
                        master = ModbusIpMaster.CreateIp(tcpClient);
                        master.Transport.Retries = 4;
                        master.Transport.ReadTimeout = 4000;
                        master.Transport.WriteTimeout = 4000;
                        master.Transport.WaitToRetryMilliseconds = 1000;
                        return master;
                    }
                    else
                    {
                        Logger.WriteDebugLog("Disconnected from network (No ping). Ping Status = " + reply.Status.ToString());
                        Thread.Sleep(1000 * 4);
                    }
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.WriteErrorLog(ex.ToString());
                }
                finally
                {

                }
            } while (reply.Status != IPStatus.Success && count < 3);
            if (netMon != null)
            {
                netMon.Dispose();
            }
            return master;
        }

        private float GetFloat(ushort[] input)
        {
            byte[] b = { (byte)(input[0] & 0xff), (byte)(input[0] >> 8), (byte)(input[1] & 0xff), (byte)(input[1] >> 8) };
            return System.BitConverter.ToSingle(b, 0);
        }
    }
}
