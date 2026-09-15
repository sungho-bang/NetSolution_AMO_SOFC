using System;

namespace SOFCMeas
{
    internal sealed class PlcGraphSnapshot
    {
        internal ushort RequestValue { get; set; }
        internal int LoadRaw { get; set; }
        internal double? LoadKgf { get; set; }
        internal ushort[] LoadWords { get; set; }
        internal bool InvalidLoadValue { get; set; }
        internal DateTime ReadTime { get; set; }
    }

    internal sealed class PlcGraphUpdate
    {
        internal bool ResetGraph { get; set; }
        internal bool AddSample { get; set; }
        internal bool FreezeGraph { get; set; }
        internal bool RequestChanged { get; set; }
        internal bool InvalidRequestValue { get; set; }
        internal bool IsCollecting { get; set; }
        internal ushort RequestValue { get; set; }
        internal int LoadRaw { get; set; }
        internal double? LoadKgf { get; set; }
        internal ushort[] LoadWords { get; set; }
        internal DateTime ReadTime { get; set; }
    }

    internal sealed class PlcGraphProtocol
    {
        private bool m_HasPreviousRequest;
        private ushort m_PreviousRequestValue;
        private bool m_Collecting;

        internal void ResetConnectionState()
        {
            m_HasPreviousRequest = false;
            m_PreviousRequestValue = 0;
            m_Collecting = false;
        }

        internal PlcGraphUpdate Process(
            PlcGraphSnapshot snapshot,
            ushort startRequestValue,
            ushort endRequestValue,
            bool collectionEnabled = true)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            PlcGraphUpdate update = new PlcGraphUpdate
            {
                RequestValue = snapshot.RequestValue,
                LoadRaw = snapshot.LoadRaw,
                LoadKgf = snapshot.LoadKgf,
                LoadWords = snapshot.LoadWords,
                ReadTime = snapshot.ReadTime
            };
            if (!collectionEnabled)
            {
                ResetConnectionState();
                return update;
            }

            update.RequestChanged = !m_HasPreviousRequest ||
                                    snapshot.RequestValue != m_PreviousRequestValue;
            m_HasPreviousRequest = true;
            m_PreviousRequestValue = snapshot.RequestValue;

            if (snapshot.RequestValue == startRequestValue)
            {
                if (!m_Collecting)
                {
                    m_Collecting = true;
                    update.ResetGraph = true;
                }

                update.AddSample = !snapshot.InvalidLoadValue;
            }
            else if (snapshot.RequestValue == endRequestValue)
            {
                if (m_Collecting)
                {
                    update.FreezeGraph = true;
                    m_Collecting = false;
                }
            }
            else
            {
                update.InvalidRequestValue = true;
            }

            update.IsCollecting = m_Collecting;

            return update;
        }
    }
}
