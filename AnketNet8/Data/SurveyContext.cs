using AnketNet8.Models;
using Microsoft.EntityFrameworkCore;

namespace AnketNet8.Data
{
    public class SurveyContext : DbContext
    {
        public SurveyContext(DbContextOptions<SurveyContext> options) : base(options)
        {
        }

        public DbSet<Survey> Surveys { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Response> Responses { get; set; }
        public DbSet<Admin> Admins { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Survey - Question iliþkisi
            modelBuilder.Entity<Survey>()
                .HasMany(s => s.Questions)
                .WithOne(q => q.Survey)
                .HasForeignKey(q => q.SurveyId);

            // Question - Response iliþkisi
            modelBuilder.Entity<Question>()
                .HasMany(q => q.Responses)
                .WithOne(r => r.Question)
                .HasForeignKey(r => r.QuestionId);


            modelBuilder.Entity<Admin>().ToTable("Admins");

        }
    }
}
