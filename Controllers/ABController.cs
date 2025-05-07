using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AICourseTester.Data;
using AICourseTester.Models;
using AICourseTester.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AICourseTester.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ABController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MainDbContext _context;

        public ABController(MainDbContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
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
        public ActionResult<List<ABNodeModel>> PostABTrainVerify(ProblemTree<ABNode> tree)
        {
            var solution = AlphaBetaService.Search(tree);
            return solution;
        }

        [Authorize, HttpGet("Test")]
        public async Task<ActionResult<AlphaBetaResponse>> GetABTest()
        {
            var fp = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (fp == null)
            {
                _context.AlphaBeta.Add(new AlphaBeta() { UserId = _userManager.GetUserId(User) });
                _context.SaveChanges();
                fp = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            }
            if (fp.IsSolved)
            {
                var problem = fp.Problem.FromJson<ProblemTree<ABNode>>();
                var solution = fp.Solution.FromJson<List<ABNodeModel>>();
                var userSolution = fp.UserSolution.FromJson<List<ABNodeModel>>();
                return new AlphaBetaResponse() { Problem = problem, Solution = solution, UserSolution = userSolution };
            }
            if (fp.Problem == null)
            {
                var problemInner = AlphaBetaService.GenerateTree(fp.TreeHeight);

                fp.Problem = problemInner.ToJson();
                _context.AlphaBeta.Update(fp);
                await _context.SaveChangesAsync();
                return new AlphaBetaResponse() { Problem = problemInner };
            }
            return new AlphaBetaResponse() { Problem = fp.Problem.FromJson<ProblemTree<ABNode>>() };
        }

        [Authorize, HttpPost("Test")]
        public async Task<ActionResult<AlphaBetaResponse>> PostABTestVerify(List<ABNodeModel> userSolution)
        {
            var fp = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (fp == null)
            {
                return NotFound();
            }
            if (fp.IsSolved)
            {
                return new AlphaBetaResponse() { Solution = fp.Solution.FromJson<List<ABNodeModel>>() };
            }
            fp.UserSolution = userSolution.ToJson();
            var problem = fp.Problem.FromJson<ProblemTree<ABNode>>();
            var solution = AlphaBetaService.Search(problem);
            fp.Solution = solution.ToJson();
            fp.IsSolved = true;

            _context.AlphaBeta.Update(fp);
            await _context.SaveChangesAsync();
            return new AlphaBetaResponse() { Problem = problem, Solution = solution };
        }

        [Authorize(Roles = "Administrator"), HttpGet("Users/")]
        public ActionResult<AlphaBeta[]?> GetUsers()
        {
            var fp = _context.AlphaBeta.Where(f => f.User.Email != "admin@admin.com").ToArray();
            return fp;
        }

        [Authorize(Roles = "Administrator"), HttpGet("Users/{userId}/")]
        public ActionResult<AlphaBetaResponse> GetUser(string userId)
        {
            var fp = _context.Fifteens.Where(f => f.UserId == userId).FirstOrDefault();
            if (fp != null)
            {
                return new AlphaBetaResponse() { Problem = fp.Problem?.FromJson<ProblemTree<ABNode>>(), Solution = fp.Solution?.FromJson<List<ABNodeModel>>(), UserSolution = fp.UserSolution?.FromJson<List<ABNodeModel>>() };
            }
            return NotFound();
        }

        [Authorize(Roles = "Administrator"), HttpPut("Users/{userId}/")]
        public async Task<ActionResult> UpdateABTest(string userId, [System.Web.Http.FromUri] int? height = null, [System.Web.Http.FromUri] bool generate = false)
        {
            var fp = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == userId);
            if (fp == null)
            {
                if (_context.Users.FirstOrDefault(f => f.Id == userId) != null)
                {
                    _context.AlphaBeta.Add(new AlphaBeta() { UserId = userId });
                    _context.SaveChanges();
                    fp = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == userId);
                }
                else
                {
                    return NotFound();
                }
            }
            if (height != null)
            {
                fp.TreeHeight = (int)height;
            }
            fp.Problem = null;
            fp.Solution = null;
            fp.IsSolved = false;
            if (generate == true)
            {
                var tree = AlphaBetaService.GenerateTree(fp.TreeHeight);
                fp.Problem = tree.ToJson();
            }
            _context.AlphaBeta.Update(fp);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [Authorize(Roles = "Administrator"), HttpDelete("Users/{userId}")]
        public async Task<ActionResult> DeleteABTest(string userId)
        {
            var fp = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == userId);
            if (fp == null) { return NotFound(); }
            fp.Problem = null;
            fp.Solution = null;
            fp.UserSolution = null;
            fp.IsSolved = false;
            _context.AlphaBeta.Update(fp);
            _context.SaveChanges();
            return Ok();
        }
    }
}
