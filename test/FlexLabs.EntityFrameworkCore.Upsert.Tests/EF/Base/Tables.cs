using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF.Base
{
    public class Book
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string[] Genres { get; set; }
    }

    public class Country
    {
        public int ID { get; set; }
        [Required, StringLength(50)]
        public string Name { get; set; }
        [Required, StringLength(2)]
        public string ISO { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
    }

    [Table("Dash-Table")]
    public class DashTable
    {
        public int ID { get; set; }
        [Column("Data-Set")]
        public string DataSet { get; set; }
        public DateTime Updated { get; set; }
    }

    public class JsonData
    {
        public int ID { get; set; }
        public string Data { get; set; }
    }

    public class PageVisit
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public DateTime Date { get; set; }
        public int Visits { get; set; }
        public DateTime FirstVisit { get; set; }
        public DateTime LastVisit { get; set; }
    }

    [Table("SchemaTable", Schema = "testsch")]
    public class SchemaTable
    {
        public int ID { get; set; }
        public int Name { get; set; }
        public DateTime Updated { get; set; }
    }

    public class Status
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime LastChecked { get; set; }
    }
}
