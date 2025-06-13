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
        public ProblemTree<ABNode> GetABTrain(int depth = 3, int max = 10, int template = 1)
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
        public ActionResult<AlphaBetaSolutionDTO> PostABTrainVerify(ProblemTree<ABNode> tree)
        {
            var solution = AlphaBetaService.Search(tree);
            return solution;
        }

        [Authorize, HttpGet("Test")]
        public async Task<ActionResult<AlphaBetaDTO>> GetABTest()
        {
            var ab = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (ab == null)
            {
                return NotFound();
            }
            if (ab.IsSolved)
            {
                return new AlphaBetaDTO() {
                    Id = ab.Id,
                    Problem = ab.Problem == null ? null : ab.Problem.FromJson<ProblemTree<ABNode>>(),
                    Solution = ab.Solution == null ? null : ab.Solution.FromJson<List<ABNodeModel>>(),
                    UserSolution = ab.UserSolution == null ? null : ab.UserSolution.FromJson<List<ABNodeModel>>(),
                    Path = ab.Path == null ? null : ab.Path.FromJson<int[]>(),
                    UserPath = ab.UserPath == null ? null : ab.UserPath.FromJson<int[]>(),
                    Date = ab.Date,
                    IsSolved = ab.IsSolved
                };
            }
            if (ab.Problem == null)
            {
                return NotFound();
            }
            return new AlphaBetaDTO()
            {
                Id = ab.Id,
                Problem = ab.Problem.FromJson<ProblemTree<ABNode>>(),
                Date = ab.Date,
                IsSolved = ab.IsSolved
            };
        }

        [Authorize, HttpPost("Test")]
        public async Task<ActionResult<AlphaBetaSolutionDTO>> PostABTestVerify(AlphaBetaSolutionDTO userSolution)
        {
            var ab = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (ab == null)
            {
                return NotFound();
            }
            if (ab.IsSolved)
            {
                return new AlphaBetaSolutionDTO() 
                { 
                    Nodes = ab.Solution == null ? null : ab.Solution.FromJson<List<ABNodeModel>>(),
                    Path = ab.Path == null ? null : ab.Path.FromJson<int[]>()
                };
            }
            ab.UserSolution = userSolution.Nodes.ToJson();
            ab.UserPath = userSolution.Path.ToJson();
            var problem = ab.Problem.FromJson<ProblemTree<ABNode>>();
            var solution = AlphaBetaService.Search(problem);
            ab.Solution = solution.Nodes.ToJson();
            ab.Path = solution.Path.ToJson();
            ab.IsSolved = true;

            _context.AlphaBeta.Update(ab);
            await _context.SaveChangesAsync();
            return solution;
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
        public async Task<ActionResult> PostFPTestAssign(string userId, int treeHeight, int max = 10, int template = 1)
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
        public async Task<ActionResult> PostFPTestAssign(int groupId, int treeHeight, int max = 10, int template = 1)
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
                    Task = new AlphaBetaDTO 
                    {
                        Problem = ab.Problem == null ? null : ab.Problem.FromJson<ProblemTree<ABNode>>(),
                        UserSolution = null,
                        Solution = null,
                        TreeHeight = ab.TreeHeight,
                        Date = ab.Date,
                        IsSolved = ab.IsSolved
                    },
                    User = u
                });
            return await ab.ToArrayAsync();
        }

        [DisableRateLimiting]
        [Authorize(Roles = "Administrator"), HttpGet("Users/{userId}/")]
        public async Task<ActionResult<AlphaBetaTaskDTO>> GetUser(string userId)
        {
            var ab = _usersService.UserLeftJoinGroup(userId).Join(_context.AlphaBeta,
                u => u.Id,
                ab => ab.UserId,
                (u, ab) => new AlphaBetaTaskDTO
                {
                    Task = new AlphaBetaDTO
                    {
                        Problem = ab.Problem == null ? null : ab.Problem.FromJson<ProblemTree<ABNode>>(),
                        UserSolution = ab.UserSolution == null ? null : ab.UserSolution.FromJson<List<ABNodeModel>>(),
                        Solution = ab.Solution == null ? null : ab.Solution.FromJson<List<ABNodeModel>>(),
                        TreeHeight = ab.TreeHeight,
                        Date = ab.Date,
                        IsSolved = ab.IsSolved
                    },
                    User = u
                });
            var task = await ab.FirstOrDefaultAsync();
            return task == null ? NotFound() : task;
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
