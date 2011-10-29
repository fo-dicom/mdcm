namespace Dicom.Codec
{
    public class DcmJpegParameters : DcmCodecParameters
    {
        #region CONSTRUCTORS

        public DcmJpegParameters()
        {
            Quality = 90;
        }

        #endregion

        #region AUTO-IMPLEMENTED PROPERTIES

        public int Quality { get; set; }

        #endregion
    }
}