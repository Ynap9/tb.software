namespace traobang.be.application.TraoBang.Dtos.PrepareData
{
    public class PrepareDataForDemoDto
    {
        public int IdPlan { get; set; }
        /// <summary>
        /// True thì checkin sẵn toàn bộ slide, không giới hạn số sinh viên demo mỗi khoa
        /// </summary>
        public bool IsCheckinFull { get; set; }
    }
}
