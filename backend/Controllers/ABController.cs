using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AICourseTester.Data;
using AICourseTester.Models;
using AICourseTester.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using AICourseTester.DTO;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AICourseTester.Controllers
{
    [EnableRateLimiting("token")]
    [Route("api/[controller]")]
    [ApiController]
    public class ABController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MainDbContext _context;
        private readonly UsersService _usersService;

        public ABController(MainDbContext context, UserManager<ApplicationUser> userManager, UsersService usersService)
        {
            _userManager = userManager;
            _context = context;
            _usersService = usersService;
        }

        [HttpGet("Train")]
        public ProblemTree<ABNode> GetABTrain([System.Web.Http.FromUri] int depth = 3)
        {
            var tree = AlphaBetaService.GenerateTree(depth);
            return tree;
        }

        //[HttpGet("Train/Test")]
        //public ActionResult<AlphaBetaResponse> GetABTrainTest()
        //{
        //    ABNode aNode = new ABNode();
        //    aNode.SubNodes = [new ABNode(), new ABNode()];
        //    int i = -1;
        //    int[] vals = [10, 5, 7, 11, 12, 8, 9, 8, 5, 12, 11, 12, 9, 8, 7, 10];
        //    foreach (var node1 in aNode.SubNodes)
        //    {
        //        node1.SubNodes = [new ABNode(), new ABNode()];
        //        foreach (var node2 in node1.SubNodes)
        //        {
        //            node2.SubNodes = [new ABNode(), new ABNode()];
        //            foreach (var node3 in node2.SubNodes)
        //            {
        //                node3.SubNodes = [new ABNode() { A = vals[++i], B = vals[i] }, new ABNode() { A = vals[++i], B = vals[i] }];
        //            }
        //        }
        //    }
        //    var tree = new ProblemTree<ABNode>() { Head = aNode };
        //    var sol = AlphaBetaService.Search((ProblemTree<ABNode>)tree.Clone());
        //    return new AlphaBetaResponse() { Problem=tree, Solution=sol };
        //}

        [HttpPost("Train")]
        public ActionResult<List<ABNodeModel>> PossustABTrainVerify(ProblemTree<ABNode> tree)
        {
            var solution = AlphaBetaService.Search(tree);
            return solution;
        }

        [Authorize, HttpGet("Test")]
        public async Task<ActionResult<AlphaBetaResponse>> GetABTest()
        {
            var ab = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (ab == null)
            {
                return NotFound();
            }
            if (ab.IsSolved)
            {
                var problem = ab.Problem.FromJson<ProblemTree<ABNode>>();
                var solution = ab.Solution.FromJson<List<ABNodeModel>>();
                var userSolution = ab.UserSolution.FromJson<List<ABNodeModel>>();
                return new AlphaBetaResponse() { Problem = problem, Solution = solution, UserSolution = userSolution };
            }
            if (ab.Problem == null)
            {
                return NotFound();
            }
            return new AlphaBetaResponse() { Problem = ab.Problem.FromJson<ProblemTree<ABNode>>() };
        }

        [Authorize, HttpPost("Test")]
        public async Task<ActionResult<AlphaBetaResponse>> PostABTestVerify(List<ABNodeModel> userSolution)
        {
            var ab = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (ab == null)
            {
                return NotFound();
            }
            if (ab.IsSolved)
            {
                return new AlphaBetaResponse() { Solution = ab.Solution.FromJson<List<ABNodeModel>>() };
            }
            ab.UserSolution = userSolution.ToJson();
            var problem = ab.Problem.FromJson<ProblemTree<ABNode>>();
            var solution = AlphaBetaService.Search(problem);
            ab.Solution = solution.ToJson();
            ab.IsSolved = true;

            _context.AlphaBeta.Update(ab);
            await _context.SaveChangesAsync();
            return new AlphaBetaResponse() { Problem = problem, Solution = solution };
        }
        private async Task<bool> _assignTask(string userId, int treeHeight)
        {
            if ((await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)) == null)
            {
                return false;
            }
            var ab = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == userId);
            if (ab == null)
            {
                _context.AlphaBeta.Add(new AlphaBeta() { UserId = userId });
                await _context.SaveChangesAsync();
                ab = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == userId);
            }
            ab.TreeHeight = treeHeight;
            ab.UserSolution = null;
            ab.Solution = null;
            ab.IsSolved = false;
            ab.Date = DateTime.Now;

            var problemInner = AlphaBetaService.GenerateTree(ab.TreeHeight);
            ab.Problem = problemInner.ToJson();
            _context.AlphaBeta.Update(ab);
            return true;
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpPost("Users/Assign")]
        public async Task<ActionResult> PostFPTestAssign(string[] userIds, int treeHeight)
        {
            foreach (var userId in userIds)
            {
                await _assignTask(userId, treeHeight);
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpPost("Users/{userId}/Assign")]
        public async Task<ActionResult> PostFPTestAssign(string userId, int treeHeight)
        {
            if (await _assignTask(userId, treeHeight))
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpPost("Groups/{groupId}/Assign")]
        public async Task<ActionResult> PostFPTestAssign(int groupId, int treeHeight)
        {
            var userIds = await _context.UserGroups.Include(ug => ug.User).Where(ug => ug.GroupId == groupId).Select(ug => ug.UserId).ToArrayAsync();
            foreach (var userId in userIds)
            {
                await _assignTask(userId, treeHeight);
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpGet("Users/")]
        public async Task<ActionResult<AlphaBetaTaskDTO[]?>> GetUsers()
        {
            var ab = _usersService.UserLeftJoinGroup().Join(_context.AlphaBeta,
                u => u.Id,
                ab => ab.UserId,
                (u, ab) => new AlphaBetaTaskDTO
                {
                    Task = ab,
                    User = u
                });
            return await ab.ToArrayAsync();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpGet("Users/{userId}/")]
        public ActionResult<AlphaBetaResponse> GetUser(string userId)
        {
            var ab = _context.AlphaBeta.Where(f => f.UserId == userId).FirstOrDefault();
            if (ab != null)
            {
                return new AlphaBetaResponse() { Problem = ab.Problem?.FromJson<ProblemTree<ABNode>>(), Solution = ab.Solution?.FromJson<List<ABNodeModel>>(), UserSolution = ab.UserSolution?.FromJson<List<ABNodeModel>>() };
            }
            return NotFound();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpPut("Users/{userId}/")]
        public async Task<ActionResult> UpdateABTest(string userId, [System.Web.Http.FromUri] int? height = null, [System.Web.Http.FromUri] bool generate = false)
        {
            var ab = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == userId);
            if (ab == null)
            {
                if (_context.Users.FirstOrDefault(f => f.Id == userId) != null)
                {
                    _context.AlphaBeta.Add(new AlphaBeta() { UserId = userId });
                    _context.SaveChanges();
                    ab = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == userId);
                }
                else
                {
                    return NotFound();
                }
            }
            if (height != null)
            {
                ab.TreeHeight = (int)height;
            }
            ab.Problem = null;
            ab.Solution = null;
            ab.IsSolved = false;
            ab.Date = DateTime.Now;
            if (generate == true)
            {
                var tree = AlphaBetaService.GenerateTree(ab.TreeHeight);
                ab.Problem = tree.ToJson();
            }
            _context.AlphaBeta.Update(ab);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpDelete("Users/{userId}")]
        public async Task<ActionResult> DeleteABTest(string userId)
        {
            await _context.AlphaBeta.Where(f => f.UserId == userId).ExecuteDeleteAsync();
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
