using AICourseTester.Controllers;
using AICourseTester.Data;
using AICourseTester.Models;
using AICourseTester.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace TestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var _userManager = new Mock<UserManager<ApplicationUser>>();
            var _context = new Mock<MainDbContext>();
            _context.Setup(x => x.AlphaBeta);
            var _usersService = new Mock<UsersService>();
            var controller = new AController(_context.Object, _userManager.Object, _usersService.Object);
            var result = controller.GetFPTrain();
            Assert.IsNotNull(result);
        }
    }
}