using HeriStep.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace HeriStep.API.Migrations
{
    [DbContext(typeof(HeriStepDbContext))]
    partial class HeriStepDbContextModelSnapshot : ModelSnapshot
    {
        // QUAN TRỌNG: BuildModel phải nằm TRONG Class này
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("HeriStep.Shared.PointOfInterest", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("Name")
                    .IsRequired()
                    .HasColumnType("nvarchar(255)")
                    .HasColumnName("name_default"); // Ánh xạ đúng với SQL

                b.Property<double>("Latitude").HasColumnType("float");
                b.Property<double>("Longitude").HasColumnType("float");

                b.Property<int>("Radius")
                    .HasColumnType("int")
                    .HasColumnName("radius_meter");

                b.Property<string>("ImageUrl")
                    .HasColumnType("nvarchar(500)")
                    .HasColumnName("image_thumb");

                b.Property<bool>("IsOpen")
                    .HasColumnType("bit")
                    .HasColumnName("is_open");

                b.Property<DateTime>("UpdatedAt")
                    .HasColumnType("datetime")
                    .HasColumnName("updated_at");

                b.HasKey("Id");
                b.ToTable("Stalls"); // Khớp với bảng Stalls trong SQL
            });

            modelBuilder.Entity("HeriStep.Shared.Tour", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("Description").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("Name").IsRequired().HasColumnType("nvarchar(max)");

                b.HasKey("Id");
                b.ToTable("Tours");
            });
#pragma warning restore 612, 618
        }
    }
}