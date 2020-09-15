using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SONA_OffsetCorrectionEWMA
{
    class DatabaseAccess
    {
		internal static List<MachineInfoDTO> GetTPMTrakMachine()
		{
			List<MachineInfoDTO> machines = new List<MachineInfoDTO>();
			string query = @"select machineid,IP,IPPortno,Interfaceid,DAPEnabled from MachineInformation where TPMTrakEnabled = 1";

			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = new SqlCommand(query, conn);
			SqlDataReader reader = default(SqlDataReader);
			try
			{
				reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
				while (reader.Read())
				{
					MachineInfoDTO machine = new MachineInfoDTO();
					machine.MachineId = reader["machineid"].ToString().Trim();
					machine.IpAddress = reader["IP"].ToString().Trim();
					machine.PortNo = Int32.Parse(reader["IPPortno"].ToString().Trim());
					machine.InterfaceId = reader["Interfaceid"].ToString().Trim();
					machine.DataCollectionProtocol = GetProtocol(reader["DAPEnabled"].ToString());

					machines.Add(machine);
				}
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.Message);
			}
			finally
			{
				if (reader != null) reader.Close();
				if (conn != null) conn.Close();
			}

			return machines;
		}

		private static string GetProtocol(string str)
		{
			string protocol = "profinet";
			switch (str)
			{
				case "0":
					protocol = "raw";
					break;
				case "1":
					protocol = "dap";
					break;
				case "2":
					protocol = "modbus";
					break;
				case "3":
					protocol = "profinet";
					break;
				case "4":
					protocol = "csv";
					break;

			}
			return protocol;
		}

        internal static void InsertDataToSPCAutoData(string mc, int partID, int operationID, string Opr, int dimension, float meanVal, float lambdaVal, float lVal, float sigmaVal, float measuredValue, float eWMAVal, float lCLVal, float uCLVal, float correctionValue, float maxCorrectionStep, DateTime measuredDateTime, int iterationCount,int ngComp,int altCorrection)
        {
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			string query = @"Insert into SPCAutoData (Mc, Comp, Opn, Opr, Dimension, Value, Timestamp, BatchTS, CorrectionValue, BatchID, Lambda, Sigma, EWMA_Zi, L, LCL, UCL, Mean, MaxCorrectionStep,NG_Component,Alternate_Correction) values (@mc, @comp, @opn, @opr, @dimension, @measuredValueXi, @measuredDateTime, @BatchTS, @correctionValue, @BatchID, @lambda, @sigma, @ewmaZi, @l, @lcl, @ucl, @mean, @maxCorrectionStep, @ng, @alt)";
            try
            {
				cmd = new SqlCommand(query, conn);
				cmd.Parameters.AddWithValue("@mc", mc);
				cmd.Parameters.AddWithValue("@comp", partID);
				cmd.Parameters.AddWithValue("@opn", operationID);
				cmd.Parameters.AddWithValue("@opr", Opr);
				cmd.Parameters.AddWithValue("@dimension", dimension);
				cmd.Parameters.AddWithValue("@mean", meanVal.ToString("00.0000"));
				cmd.Parameters.AddWithValue("@lambda", lambdaVal.ToString("00.0000"));
				cmd.Parameters.AddWithValue("@l", lVal.ToString("00.0000"));
				cmd.Parameters.AddWithValue("@sigma", sigmaVal.ToString("00.0000"));
				cmd.Parameters.AddWithValue("@measuredValueXi", measuredValue.ToString("00.0000"));
				cmd.Parameters.AddWithValue("@ewmaZi", eWMAVal.ToString("00.0000"));
				cmd.Parameters.AddWithValue("@lcl", lCLVal.ToString("00.0000"));
				cmd.Parameters.AddWithValue("@ucl", uCLVal.ToString("00.0000"));
				cmd.Parameters.AddWithValue("@correctionValue", correctionValue.ToString("00.0000"));
				cmd.Parameters.AddWithValue("@maxCorrectionStep", maxCorrectionStep);
				cmd.Parameters.AddWithValue("@measuredDateTime", measuredDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
				cmd.Parameters.AddWithValue("@BatchTS", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				cmd.Parameters.AddWithValue("@BatchID", iterationCount);
				cmd.Parameters.AddWithValue("@ng", ngComp);
				cmd.Parameters.AddWithValue("@alt", altCorrection);
				int result=cmd.ExecuteNonQuery();

                if (result >= 0)
                {
					Logger.WriteDebugLog("Data Inserted To SPCAutoData successfully");
                }
                else
                {
					Logger.WriteDebugLog("Data Inserttion To SPCAutoData Failed");
				}
			}
			catch(Exception ex)
            {
				Logger.WriteErrorLog("Exception IN DatabaseAccess.InsertDataToSPCAutoData() : " + ex.Message);
            }
            finally
            {
				if (conn != null) conn.Close();
            }
        }
    }
}
