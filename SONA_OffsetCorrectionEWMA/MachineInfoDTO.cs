using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SONA_OffsetCorrectionEWMA
{
    class MachineInfoDTO
    {
		#region private
		private string _ip;
		private int _portNo;
		private string _interfaceId;
		private string _machineId;
		private string _dataCollectionProtocol;
		private string _machinePath;

		#endregion

		public string IpAddress
		{
			get { return _ip; }
			set { _ip = value; }
		}

		public string MachinePath
		{
			get { return _machinePath; }
			set { _machinePath = value; }
		}

		public int PortNo
		{
			get { return _portNo; }
			set { _portNo = value; }
		}
		public string MachineId
		{
			get { return _machineId; }
			set { _machineId = value; }
		}

		public string InterfaceId
		{
			get { return _interfaceId; }
			set { _interfaceId = value; }
		}

		public string DataCollectionProtocol
		{
			get { return _dataCollectionProtocol; }
			set { _dataCollectionProtocol = value; }
		}
	}
}
