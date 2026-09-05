namespace PcToolkit
{
    public struct MemReport
    {
        public double TotalGB;
        public double AvailableGB;
        public double FreeGB;
        public double StandbyGB;
        public double ModifiedGB;
        public double CommittedGB;
        public double PoolPagedGB;
        public double PoolNonPagedGB;
        public bool DetalheOk;
    }
}
