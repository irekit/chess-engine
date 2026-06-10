using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public static class Pieces
{
    public const int None = 0;
    public const int Pawn = 1;
    public const int Knight = 2;
    public const int Bishop = 3;
    public const int Rook = 4;
    public const int Queen = 5;
    public const int King = 6;
    public const int White = 8;
    public const int Black = 16;
}

public class manager : MonoBehaviour
{
    public class PositionNode
    {
        public PositionNode[] children;
        public int visits;
        public float[] evaluations;
        public float evaluation;
        public float wins;
        public float winsits;
        public PositionNode()
        {
            visits = 0;
            wins = 0;
            evaluation = 0;
            winsits = 0;
        }
    }
   
    [SerializeField] private float[] pawntable;
    [SerializeField] private float[] kingtable;
    [SerializeField] private float[] rooktable;
    [SerializeField] private float[] bishoptable;
    [SerializeField] private float[] knighttable;
    [SerializeField] private float[] pawntable2;
    [SerializeField] private float[] kingtable2;
    [SerializeField] private float[] rooktable2;
    [SerializeField] private float[] bishoptable2;
    [SerializeField] private float[] knighttable2;
    [SerializeField] private GameObject evalbar;
    private float trashcount = 0;
    float Evaluate()
    {
        if (white_win && !black_win)
        {
            return white_move ? 200 : -200;
        }
        else if (black_win && !white_win)
        {
            return white_move ? -200 : 200;
        }
        else if(black_win && white_win)
        {
            return 0;
        }
        float eval = 0;
        float mobilitydiff = legalmoves.Count - trashcount;
        if (white_move)
        {
            eval += mobilitydiff * 0.05f;
        }
        else
        {
            eval -= mobilitydiff * 0.05f;
        }
        int num_pieces = 0;
        for (int i = 0; i < 64; i++)
        {
            if ((pieces[i] & 7) != 0)
            {
                num_pieces++;
            }
        }
        float taper = 1 - (num_pieces / 32.0f);
        for(int i = 0; i < 64; i++)
        {
            float plv = piece_values[pieces[i] & 7];
            bool piecewhite = (pieces[i] & ~7) == 8;
            int piecenum = pieces[i] & 7;
            if(piecenum == 0)
            {
                continue;
            }
            if (piecewhite != white_move)
            {

                if (piecewhite)
                {
                    if (attacked_white[i] > 0 && attacked_black[i] == 0)
                    {
                        //undefended
                        plv = plv * 0.1f;
                    }
                    else if (attacked_white[i] > 0 && attacked_black[i] > 0)
                    {
                        //defended
                        plv = Mathf.Min(plv, attacked_white[i]);
                    }
                }
                else
                {
                    if (attacked_black[i] > 0 && attacked_white[i] == 0)
                    {
                        //undefended
                        plv = plv * 0.1f;
                    }
                    else if (attacked_black[i] > 0 && attacked_white[i] > 0)
                    {
                        //defended
                        plv = Mathf.Min(plv, attacked_black[i]);
                    }
                }
            }
            int ee = piecewhite ? i : 63 - i;
            if (piecenum == 1)
            {
                plv += Mathf.Lerp(pawntable[ee], pawntable2[ee], taper) * 0.2f;
            }
            else if (piecenum == Pieces.Rook)
            {
                plv += Mathf.Lerp(rooktable[ee], rooktable2[ee], taper) * 0.2f;
            }
            else if (piecenum == Pieces.Bishop)
            {
                plv += Mathf.Lerp(bishoptable[ee], bishoptable2[ee], taper) * 0.2f;
            }
            else if(piecenum == Pieces.Knight)
            {
                plv += Mathf.Lerp(knighttable[ee], knighttable2[ee], taper) * 0.2f;
            }
            else if(piecenum == 6)
            {
                plv += Mathf.Lerp(kingtable[ee], kingtable2[ee], taper) * 0.2f;
            }
            if (pinned_pieces[i] != 0)
            {
                if (piecewhite)
                {
                    eval -= 1;
                }
                else
                {
                    eval += 1;
                }
            }
            if (piecewhite)
            {

                eval += plv;
            }
            else
            {
                
                eval -= plv;
            }

        }
        if (!white_move)
        {
            eval = -eval;
        }
        return eval;
    }
   
    public struct Move
    {
        public int start;
        public int end;
        public int promotion_piece;
        public bool passant_target;
        public int capture_piece;
        public bool castling_before;
    }
    public struct Position
    {
        public byte[] pieces;
        public bool castlable_white;
        public bool castlable_black;
        public Move lastmove;
        public Move two_moves;
        public bool white_move;
        public bool Compare(Position other)
        {
            bool equals = true;
            for(int i = 0; i < 64; i++)
            {
                if (pieces[i] != other.pieces[i])
                {
                    equals = false;
                }
            }
            equals = castlable_white == other.castlable_white && equals && castlable_black == other.castlable_black && white_move == other.white_move;
            equals = equals && lastmove.passant_target == other.lastmove.passant_target;
            return equals;
        }
    }
    [SerializeField] private GameObject[] renderers;
    [SerializeField] private Sprite[] whitesprites;
    [SerializeField] private Sprite[] blacksprites;
    [SerializeField] private Camera maincamera;
    private bool castlable_white = true;
    private bool castlable_black = true;
    static readonly int[] piece_values = { 
        0,
        1,
        3,
        3,
        5,
        9,
        100
    };
    private bool white_move;
    private int[] pieces;
    private Move two_moves;
    List<Move> legalmoves;
    private bool promoting = false;
    [SerializeField] private GameObject white_promotion;
    [SerializeField] private GameObject black_promotion;
    [SerializeField] private GameObject[] highlight_squares;
    [SerializeField] private Material eval_bar;
    private int promotion_square;
    private int promotion_start;
    private List<Position> last_previous_positions;
    public void Promote(int promotion)
    {
        Move promo = new Move();
        pieces[promotion_start] = pieces[promotion_square];
        pieces[promotion_square] = Pieces.White | Pieces.Pawn;
        promo.start = promotion_start;
        promo.end = promotion_square;
        promo.promotion_piece = promotion;
        if (white_move) promo.castling_before = castlable_white;
        else promo.castling_before = castlable_black;
        promo.capture_piece = promo_capture;
        MovePiece(promo);
        promoting = false;
        white_promotion.SetActive(false);
        black_promotion.SetActive(false);
    }
    private Position last_position;
    const float inv_temp = 1f;
    const float c_val = 1.3f;
    const int samples = 400;
    const float noise_magn = 0.05f;
    Move GenerateMove()
    {
        bool this_forced = ForcedWin(white_move);
        search = true;
        float tim = Time.realtimeSinceStartup;
        List<PositionNode> prev_nodes = new List<PositionNode>();
        bool debug_this = false;

        last_position = StorePosition();
        last_previous_positions.Clear();
        last_previous_positions.AddRange(previous_positions);
        PositionNode node = new PositionNode();
        node.visits += 1;
        node.children = new PositionNode[legalmoves.Count];
        PositionNode current = node;
        int this_samples = persist.samples;
        float last_rand = -1;
        int[] each_visits = new int[legalmoves.Count];
        for (int i = 0; i < this_samples; i++)
        {
            node.visits += 1;
            int game_length = 0;
            prev_nodes.Clear();
            int last_visits = node.visits;
            bool firstlooper = true;
            while (!white_win && !black_win)
            {
                game_length++;
                bool curr_evals = current.evaluations != null;
                if (!curr_evals)
                {
                    current.evaluations = new float[legalmoves.Count];
                }
                float current_eval = 0;
                current_eval = Evaluate();
                float max_ucb = -100;

                List<int> max_inds = new List<int>();
                for (int t = 0; t < legalmoves.Count; t++)
                {
                    Move move = legalmoves[t];
                    MovePiece(move);
                    float cur_visits = 1;
                    float win_probability = 0;
                    float node_evaluation = 0;
                    if (curr_evals)
                    {
                        node_evaluation = current.evaluations[t];
                    }
                    else
                    {
                        node_evaluation = Evaluate();
                //node evaluation is for the side to move next, a pure evaluation without sigmoid scaling
                        current.evaluations[t] = node_evaluation;
                    }
                    if (current.children[t] != null)
                    {
                        cur_visits = current.children[t].visits;
                        win_probability = (cur_visits + current.children[t].wins) / (float)(cur_visits * 2);
                        node_evaluation = current.children[t].evaluation;
                    }
                    float gaussian_noise = 0;
                    if (last_rand == -1)
                    {

                        float u1, u2, w;
                        do
                        {
                            u1 = UnityEngine.Random.Range(-1f, 1f);
                            u2 = UnityEngine.Random.Range(-1f, 1f);
                            w = u1 * u1 + u2 * u2;
                        } while (w >= 1.0f || w == 0f);

                        w = Mathf.Sqrt((-2.0f * Mathf.Log(w)) / w);
                        gaussian_noise = u1 * w;
                        last_rand = u2 * w;
                    }
                    else
                    {
                        gaussian_noise = last_rand;
                        last_rand = -1;
                    }
                    node_evaluation += gaussian_noise * noise_magn;
                    //node evaluation is actually opposite to current evaluation
                    float winpr = 1 / (1 + Mathf.Pow(10, node_evaluation * 0.25f));
                    //upper confidencee bound
                    float ucb = Mathf.Lerp(win_probability, winpr, Mathf.Clamp(1.0498f - Mathf.Exp(0.5f * current.winsits - 3.5f), 0, 1)) + c_val * Mathf.Sqrt(Mathf.Log(last_visits) / cur_visits);
                    if(debug_this)Debug.Log("win probability for " + move.start + " to " + move.end + " is " + winpr);
                    if(ucb > max_ucb)
                    {
                        max_ucb = ucb;
                        max_inds.Clear();
                        max_inds.Add(t);
                    }
                    if(ucb == max_ucb)
                    {
                        max_inds.Add(t);
                    }
                    UnMovePiece(move);
                }
                int max_ind = max_inds[UnityEngine.Random.Range(0, max_inds.Count)];
                if (current.children[max_ind] == null)
                {
                    current.children[max_ind] = new PositionNode();
                }
                current.children[max_ind].evaluation = current.evaluations[max_ind];
                if (firstlooper)
                {
                    each_visits[max_ind] += 1;
                    firstlooper = false;
                }
                current = current.children[max_ind];
                prev_nodes.Add(current);
                current.visits += 1;
                if(debug_this) Debug.Log("I choose to " + legalmoves[max_ind].start + " to " + legalmoves[max_ind].end);
                MovePiece(legalmoves[max_ind]);
                if (current.children == null)
                {
                    current.children = new PositionNode[legalmoves.Count];
                }
                if (!this_forced)
                {
                    bool now_forc = ForcedWin(white_move);
                    if (now_forc)
                    {
                        if (white_move)
                        {
                            white_win = true;
                        }
                        else
                        {
                            black_win = true;
                        }
                    }
                }
                last_visits = current.visits;
                        
            }
            
            int who_won = 0;
            if (!last_position.white_move)
            {
                if(white_win && !black_win)
                {
                    who_won = 1;
                    //Debug.Log("game won");
                }
                else if(black_win && !white_win)
                {
                    who_won = -1;
                    //Debug.Log("game lost");
                }
                else
                {
                    //Debug.Log("game draw");
                }
            }
            else
            {
                if (white_win && !black_win)
                {
                    who_won = -1;
                    //Debug.Log("game lost");
                }
                else if (black_win && !white_win)
                {
                    who_won = 1;
                    //Debug.Log("game won");
                }
                else
                {
                    // Debug.Log("game draw");
                }
            }
            if(debug_this) Debug.Log("rollout end, " + who_won + " to my side won");
            //if the next move in the tree is mine or not
            int my_move = 0;
            //white move and last white move are both inverted
            if (white_move == last_position.white_move) {
                my_move = 1;
            }
            else
            {
                my_move = -1;
            }
            float backpropper = who_won * 50;
            for(int p = prev_nodes.Count - 1; p >= 0; p--)
            {
                current = prev_nodes[p];
                float curvv = current.evaluation;
                if (my_move < 0)
                {
                    curvv = -curvv;
                }
                backpropper = Mathf.Lerp(curvv, backpropper, 0.3f);
                if(my_move < 0)
                {
                    current.evaluation = -backpropper;
                }
                else
                {
                    current.evaluation = backpropper;
                }
                current.winsits += Mathf.Exp(-0.03f * (float)(prev_nodes.Count - p - 1));
                if (my_move == who_won)
                {
                    current.wins += Mathf.Exp(-0.03f * (float)(prev_nodes.Count - p - 1));
                }
                else if(who_won != 0)
                {
                    current.wins -= Mathf.Exp(-0.03f * (float)(prev_nodes.Count - p - 1));
                }
                my_move = -my_move;
            }
                
            previous_positions.Clear();
            previous_positions.AddRange(last_previous_positions);
            current = node;
            white_win = false;
            black_win = false;
            GetPosition(last_position);
            List<Move> lst_move = GenerateMoves(!white_move);
            legalmoves = GenerateMoves(white_move);
            GetPosition(last_position);
            previous_positions.Clear();
            previous_positions.AddRange(last_previous_positions);
            List<Move> list_move = GenerateMoves(!white_move);
            legalmoves = GenerateMoves(white_move);

        }
        search = false;
        List<int> max_indes = new List<int>();
        int max_visits = 0;
        //random max move
        for(int i = 0; i < legalmoves.Count; i++)
        {
            if(each_visits[i] == max_visits)
            {
                max_indes.Add(i);
            }
            else if (each_visits[i] > max_visits)
            {
                max_indes.Clear();
                max_visits = each_visits[i];
                max_indes.Add(i);
            }
        }
        Debug.Log(Time.realtimeSinceStartup - tim);
        return legalmoves[max_indes[UnityEngine.Random.Range(0, max_indes.Count)]];
    }
    private int[] attacked_white;
    private int[] attacked_black;
    private int[] pinned_pieces;
    private List<Position> previous_positions;
    private Position StorePosition()
    {
        Position position;
        position.pieces = PiecesToBytes();
        position.lastmove = lastmove;
        position.white_move = white_move;
        position.two_moves = two_moves;
        position.castlable_white = castlable_white;
        position.castlable_black = castlable_black;
        return position;
    }
    private void GetPosition(Position position)
    {
        LoadBytes(position.pieces);
        lastmove = position.lastmove;
        two_moves = position.two_moves;
        white_move = position.white_move;
        castlable_white = position.castlable_white;
        castlable_black = position.castlable_black;
    }
    List<Move> GenerateMoves(bool for_white)
    {
        List<Move> moves = new List<Move>();
        bool check = false;
        int king_index = 0;
        for (int i = 0; i < 64; i++)
        {
            if (((pieces[i] & ~ 7) == 8) != for_white)
            {
                pinned_pieces[i] = 0;

            }
            if (for_white) attacked_black[i] = 0;
            else attacked_white[i] = 0;
        }
        for (int i = 0; i < 64; i++)
        {
            int piece = pieces[i];
            bool white = (piece & ~7) == 8;
            int piecename = piece & 7;
            if (piecename != 0 && white == for_white)
            {
                if (piecename == 1)
                {
                    if ((white && (pieces[i + 8] & 7) == 0) || (!white && (pieces[i - 8] & 7) == 0))
                    {
                        Move move = new Move();
                        move.start = i;
                        if (white_move) move.castling_before = castlable_white;
                        else move.castling_before = castlable_black;
                        move.capture_piece = 0;
                        int new_square = 0;
                        if (white)
                        {
                            new_square = i + 8;
                        }
                        else
                        {
                            new_square = i - 8;
                        }
                        move.end = new_square;
                        if (new_square / 8 == 0 || new_square / 8 == 7)
                        {
                            move.promotion_piece = 2;
                            for (int j = 3; j <= 5; j++)
                            {
                                Move promo = new Move();
                                promo.promotion_piece = j;
                                promo.start = i;
                                promo.end = new_square;
                                if (white_move) promo.castling_before = castlable_white;
                                else promo.castling_before = castlable_black;
                                promo.capture_piece = pieces[promo.end] & 7;
                                moves.Add(promo);
                            }
                        }
                        moves.Add(move);
                        if ((white && (i < 16 && (pieces[i + 16] & 7) == 0)) || (!white && (i >= 48 && (pieces[i - 16] & 7) == 0)))
                        {
                            Move twosquares = new Move();
                            twosquares.passant_target = true;
                            
                            twosquares.start = i;
                            if (white)
                            {

                                twosquares.end = i + 16;
                            }
                            else
                            {
                                twosquares.end = i - 16;
                            }
                            if (white_move) twosquares.castling_before = castlable_white;
                            else twosquares.castling_before = castlable_black;
                            twosquares.capture_piece = pieces[twosquares.end] & 7;
                            moves.Add(twosquares);
                        }
                    }
                    if (white)
                    {
                        if(Mathf.Abs(i / 8 - (i + 7) / 8) == 1)
                        {
                            attacked_black[i + 7] += 1;
                        }
                        if (((pieces[i + 7] & 7) != 0 || ((pieces[i - 1] & 7) == 1 && ((pieces[i - 1] & ~7) == 8) != white && lastmove.start >= 0 && lastmove.start == i + 15 && lastmove.end == i - 1)) && !(((pieces[i + 7] & ~7) == 8) == white && (pieces[i + 7] & 7) != 0) && Mathf.Abs(i / 8 - (i + 7) / 8) == 1)
                        {
                            Move pawncapture = new Move();
                            pawncapture.start = i;
                            pawncapture.end = i + 7;
                            if (white_move) pawncapture.castling_before = castlable_white;
                            else pawncapture.castling_before = castlable_black;
                            pawncapture.capture_piece = pieces[pawncapture.end] & 7;
                            if ((i + 7) / 8 == 0 || (i + 7) / 8 == 7)
                            {
                                pawncapture.promotion_piece = 2;
                                for (int j = 3; j <= 5; j++)
                                {
                                    Move promo = new Move();
                                    promo.promotion_piece = j;
                                    promo.start = i;
                                    promo.end = i + 7;
                                    if (white_move) promo.castling_before = castlable_white;
                                    else promo.castling_before = castlable_black;
                                    promo.capture_piece = pieces[promo.end] & 7;
                                    moves.Add(promo);
                                }
                            }
                            moves.Add(pawncapture);
                        }
                        if (Mathf.Abs(i / 8 - (i + 9) / 8) == 1 && i + 9 < 64)
                        {
                            attacked_black[i + 9]++;
                        }
                        if (i + 9 < 64 && (((pieces[i + 9] & 7) != 0) || ((pieces[i + 1] & 7) == 1 && ((pieces[i + 1] & ~7) == 8) != white && lastmove.start >= 0 && lastmove.start == i + 17 && lastmove.end == i + 1)) && !(((pieces[i + 9] & ~7) == 8) == white && (pieces[i + 9] & 7) != 0) && Mathf.Abs(i / 8 - (i + 9) / 8) == 1)
                        {
                            Move pawncapture = new Move();
                            pawncapture.start = i;
                            pawncapture.end = i + 9;
                            if (white_move) pawncapture.castling_before = castlable_white;
                            else pawncapture.castling_before = castlable_black;
                            pawncapture.capture_piece = pieces[pawncapture.end] & 7;
                            if ((i + 9) / 8 == 0 || (i + 9) / 8 == 7)
                            {
                                pawncapture.promotion_piece = 2;
                                for (int j = 3; j <= 5; j++)
                                {
                                    Move promo = new Move();
                                    promo.promotion_piece = j;
                                    promo.start = i;
                                    promo.end = i + 9;
                                    if (white_move) promo.castling_before = castlable_white;
                                    else promo.castling_before = castlable_black;
                                    promo.capture_piece = pieces[promo.end] & 7;
                                    moves.Add(promo);
                                }
                            }
                            moves.Add(pawncapture);
                        }

                    }
                    else
                    {
                        if (Mathf.Abs(i / 8 - (i - 7) / 8) == 1)
                        {
                            attacked_white[i - 7]++;
                        }
                        if (((pieces[i - 7] & 7) != 0 || ((pieces[i + 1] & 7) == 1 && ((pieces[i + 1] & ~7) == 8) != white && lastmove.start >= 0 && lastmove.start == i - 15 && lastmove.end == i + 1)) && !(((pieces[i - 7] & ~7) == 8) == white && (pieces[i - 7] & 7) != 0) && Mathf.Abs(i / 8 - (i - 7) / 8) == 1)
                        {
                            Move pawncapture = new Move();
                            pawncapture.start = i;
                            pawncapture.end = i - 7;
                            if (white_move) pawncapture.castling_before = castlable_white;
                            else pawncapture.castling_before = castlable_black;
                            pawncapture.capture_piece = pieces[pawncapture.end] & 7;
                            if ((i - 7) / 8 == 0 || (i - 7) / 8 == 7)
                            {
                                pawncapture.promotion_piece = 2;
                                for (int j = 3; j <= 5; j++)
                                {
                                    Move promo = new Move();
                                    promo.promotion_piece = j;
                                    promo.start = i;
                                    promo.end = i - 7;
                                    if (white_move) promo.castling_before = castlable_white;
                                    else promo.castling_before = castlable_black;
                                    promo.capture_piece = pieces[promo.end] & 7;
                                    moves.Add(promo);
                                }
                            }
                            moves.Add(pawncapture);
                        }
                        if (Mathf.Abs(i / 8 - (i - 9) / 8) == 1 && i - 9 >= 0)
                        {
                            attacked_white[i - 9]++;
                        }
                        if (i - 9 >= 0 && ((pieces[i - 9] & 7) != 0 || ((pieces[i - 1] & 7) == 1 && ((pieces[i - 1] & ~7) == 8) != white && lastmove.start >= 0 && lastmove.start == i - 17 && lastmove.end == i - 1)) && !(((pieces[i - 9] & ~7) == 8) == white && (pieces[i - 9] & 7) != 0) && Mathf.Abs(i / 8 - (i - 9) / 8) == 1)
                        {
                            Move pawncapture = new Move();
                            pawncapture.start = i;
                            pawncapture.end = i - 9;
                            if (white_move) pawncapture.castling_before = castlable_white;
                            else pawncapture.castling_before = castlable_black;
                            pawncapture.capture_piece = pieces[pawncapture.end] & 7;
                            if ((i - 9) / 8 == 0 || (i - 9) / 8 == 7)
                            {
                                pawncapture.promotion_piece = 2;
                                for (int j = 3; j <= 5; j++)
                                {
                                    Move promo = new Move();
                                    promo.promotion_piece = j;
                                    promo.start = i;
                                    promo.end = i - 9;
                                    if (white_move) promo.castling_before = castlable_white;
                                    else promo.castling_before = castlable_black;
                                    promo.capture_piece = pieces[promo.end] & 7;
                                    moves.Add(promo);
                                }
                            }
                            moves.Add(pawncapture);
                        }
                    }
                }
                else if (piecename == 2)
                {
                    int[] verticaloffsets = { 8, 8, -8, -8, 16, 16, -16, -16 };
                    int[] horizontaloffsets = { 2, -2, 2, -2, 1, -1, 1, -1 };
                    for (int j = 0; j < 8; j++)
                    {
                        int original_rank = i / 8;
                        int vertig = i + verticaloffsets[j];
                        int full = vertig + horizontaloffsets[j];
                        if(Mathf.Floor((float)full / 8) - original_rank == verticaloffsets[j] / 8 && full >= 0 && full < 64)
                        {
                            if (for_white)
                            {
                                attacked_black[full] += piece_values[piecename];
                            }
                            else
                            {
                                attacked_white[full] += piece_values[piecename];
                            }
                            if (!(((pieces[full] & ~7) == 8) == white && (pieces[full] & 7) != 0))
                            {
                                Move knightmove = new Move();
                                knightmove.start = i;
                                knightmove.end = full;
                                if (white_move) knightmove.castling_before = castlable_white;
                                else knightmove.castling_before = castlable_black;
                                knightmove.capture_piece = pieces[knightmove.end] & 7;
                                moves.Add(knightmove);
                            }
                        }
                    }
                }
                else if (piecename == 3)
                {
                    int[] bishopoffsets = { 7, -7, 9, -9 };
                    for (int t = 0; t < 4; t++)
                    {
                        int current = i;
                        int last_piece = -1;
                        for (int j = 0; j < 8; j++)
                        {
                            int currank = current / 8;
                            current += bishopoffsets[t];
                            int newrank = current / 8;
                            if (Mathf.Abs(newrank - currank) != 1 || current < 0 || current >= 64) break;
                            bool currwhite = (pieces[current] & ~7) == 8;
                            if(last_piece == -1)
                            {
                                if (for_white)
                                {
                                    attacked_black[current] += piece_values[piecename];
                                }
                                else
                                {
                                    attacked_white[current] += piece_values[piecename];
                                }
                                if ((pieces[current] & 7) != 0 && currwhite == white)
                                {
                                    break;
                                }
                                Move bish = new Move();
                                bish.start = i;
                                bish.end = current;
                                if (white_move) bish.castling_before = castlable_white;
                                else bish.castling_before = castlable_black;
                                bish.capture_piece = pieces[bish.end] & 7;
                                moves.Add(bish);

                            }
                            else if ((pieces[current] & 7) != 0 && currwhite == white)
                            {
                                break;
                            }
                            if ((pieces[current] & 7) != 0 && currwhite != white)
                            {
                                if ((pieces[current] & 7) == 6)
                                {
                                    if (last_piece != -1)
                                    {
                                        pinned_pieces[last_piece] = t % 2 + 1;
                                        break;
                                    }
                                }
                                else if (last_piece == -1)
                                {
                                    last_piece = current;
                                }
                                else break;
                            }

                        }
                    }
                }
                else if (piecename == 4)
                {
                    int[] rookoffsets = { 8, -8, 1, -1 };
                    for (int t = 0; t < 4; t++)
                    {
                        int current = i;
                        int last_piece = -1;
                        bool passant_pin = false;
                        for (int j = 0; j < 8; j++)
                        {
                            int og_rank = current / 8;
                            current += rookoffsets[t];
                            if ((Mathf.Abs(rookoffsets[t]) == 1 && current / 8 != og_rank) || current < 0 || current >= 64)
                            {
                                break;
                            }
                            bool currwhite = (pieces[current] & ~7) == 8;
                            if (last_piece == -1)
                            {
                                if (for_white)
                                {
                                    attacked_black[current] += piece_values[piecename];
                                }
                                else
                                {
                                    attacked_white[current] += piece_values[piecename];
                                }
                                if ((pieces[current] & 7) != 0 && currwhite == white)
                                {
                                    if ((pieces[current] & 7) == 6 && (white ? castlable_white : castlable_black) && !check)
                                    {
                                        if ((white && attacked_white[current - rookoffsets[t]] == 0 && attacked_white[current - 2 * rookoffsets[t]] == 0) || (!white && attacked_black[current - rookoffsets[t]] == 0 && attacked_black[current - 2 * rookoffsets[t]] == 0))
                                        {
                                            Move castl = new Move();
                                            castl.start = current;
                                            castl.castling_before = true;
                                            castl.capture_piece = 0;
                                            int endsquare = current - rookoffsets[t] * 2;
                                            if ((Mathf.Floor((float)endsquare / 8) != og_rank && Mathf.Abs(rookoffsets[t]) == 1) || endsquare < 0 || endsquare >= 64)
                                            {
                                                break;
                                            }
                                            castl.end = endsquare;
                                            moves.Add(castl);
                                            
                                        }
                                        break;
                                    }
                                    else if(lastmove.passant_target && lastmove.end == current)
                                    {
                                        if (current + rookoffsets[t] >= 0 && current + rookoffsets[t] < 64 && (pieces[current + rookoffsets[t]] & 7) == 1)
                                        {
                                            passant_pin = true;
                                        }
                                        else break;
                                    }
                                    else break;
                                }
                                Move roo = new Move();
                                roo.start = i;
                                roo.end = current;
                                if (white_move) roo.castling_before = castlable_white;
                                else roo.castling_before = castlable_black;
                                roo.capture_piece = pieces[roo.end] & 7;
                                moves.Add(roo);

                            }
                            else if ((pieces[current] & 7) != 0 && currwhite == white)
                            {
                                if (lastmove.passant_target && lastmove.end == current && last_piece >= 0 && (pieces[last_piece] & 7) == 1 && last_piece + rookoffsets[t] == current)
                                {
                                    passant_pin = true;
                                }
                                else break;
                            }
                            if ((pieces[current] & 7) != 0 && currwhite != white)
                            {
                                if ((pieces[current] & 7) == 6)
                                {
                                    if (last_piece != -1)
                                    {
                                        pinned_pieces[last_piece] = t / 2 + 3;
                                        if (passant_pin)
                                        {
                                            pinned_pieces[last_piece] = 10;
                                        }
                                        break;
                                    }
                                }
                                else if (last_piece == -1)
                                {
                                    last_piece = current;
                                }
                                else break;
                            }

                        }
                    }
                }
                else if (piecename == 5)
                {
                    int[] rookoffsets = { 8, -8, 1, -1 };
                    for (int t = 0; t < 4; t++)
                    {
                        int current = i;
                        int last_piece = -1;
                        bool passant_pin = false;
                        for (int j = 0; j < 8; j++)
                        {
                            int og_rank = current / 8;
                            current += rookoffsets[t];
                            if ((Mathf.Abs(rookoffsets[t]) == 1 && current / 8 != og_rank) || current < 0 || current >= 64)
                            {
                                break;
                            }
                            bool currwhite = (pieces[current] & ~7) == 8;
                            if (last_piece == -1)
                            {
                                if (for_white)
                                {
                                    attacked_black[current] += piece_values[piecename];
                                }
                                else
                                {
                                    attacked_white[current] += piece_values[piecename];
                                }
                            }
                            if ((pieces[current] & 7) != 0 && currwhite == white)
                            {
                                if (lastmove.passant_target && lastmove.end == current)
                                {
                                    if (last_piece >= 0 && (pieces[last_piece] & 7) == 1 && last_piece + rookoffsets[t] == current)
                                    {
                                        passant_pin = true;
                                    }
                                    else if (current + rookoffsets[t] >= 0 && current + rookoffsets[t] < 64 && (pieces[current + rookoffsets[t]] & 7) == 1)
                                    {
                                        passant_pin = true;
                                    }
                                    else break;
                                }
                                else break;
                            }
                            if (last_piece == -1)
                            {
                                Move roo = new Move();
                                roo.start = i;
                                roo.end = current;
                                if (white_move) roo.castling_before = castlable_white;
                                else roo.castling_before = castlable_black;
                                roo.capture_piece = pieces[roo.end] & 7;
                                moves.Add(roo);

                            }
                            
                            if ((pieces[current] & 7) != 0 && currwhite != white)
                            {
                                if ((pieces[current] & 7) == 6)
                                {
                                    if (last_piece != -1)
                                    {
                                        pinned_pieces[last_piece] = t / 2 + 3;
                                        if (passant_pin)
                                        {
                                            pinned_pieces[last_piece] = 10;
                                        }
                                        break;
                                    }
                                }
                                else if (last_piece == -1)
                                {
                                    last_piece = current;
                                }
                                else break;
                            }

                        }
                    }
                    int[] bishopoffsets = { 7, 9, -7, -9 };
                    for (int t = 0; t < 4; t++)
                    {
                        int last_piece = -1;
                        int current = i;
                        for (int j = 0; j < 8; j++)
                        {
                            int currank = current / 8;
                            current += bishopoffsets[t];
                            int newrank = current / 8;
                            if (Mathf.Abs(newrank - currank) != 1 || current < 0 || current >= 64) break;
                            bool currwhite = (pieces[current] & ~7) == 8;
                            if (last_piece == -1)
                            {
                                if (for_white)
                                {
                                    attacked_black[current] += piece_values[piecename];
                                }
                                else
                                {
                                    attacked_white[current] += piece_values[piecename];
                                }
                                if ((pieces[current] & 7) != 0 && currwhite == white)
                                {
                                    break;
                                }
                                Move bish = new Move();
                                bish.start = i;
                                bish.end = current;
                                if (white_move) bish.castling_before = castlable_white;
                                else bish.castling_before = castlable_black;
                                bish.capture_piece = pieces[bish.end] & 7;
                                moves.Add(bish);

                            }
                            else if ((pieces[current] & 7) != 0 && currwhite == white)
                            {
                                break;
                            }
                            if ((pieces[current] & 7) != 0 && currwhite != white)
                            {
                                if ((pieces[current] & 7) == 6)
                                {
                                    if (last_piece != -1)
                                    {
                                        pinned_pieces[last_piece] = t % 2 + 1;
                                    }
                                }
                                else if (last_piece == -1)
                                {
                                    last_piece = current;
                                }
                                else break;
                            }

                        }
                    }
                }
                else if (piecename == 6)
                {
                    king_index = i;
                    if ((for_white && attacked_white[i] > 0) || (!for_white && attacked_black[i] > 0))
                    {
                        check = true;
                    }
                    int[] verticaloffsets = { 8, 8, 8, 0, 0, -8, -8, -8 };
                    int[] horizontaloffsets = { 1, 0, -1, 1, -1, 1, 0, -1 };
                    for (int j = 0; j < 8; j++)
                    {
                        int original_rank = i / 8;
                        int vertig = i + verticaloffsets[j];
                        int full = vertig + horizontaloffsets[j];
                        if(Mathf.Floor((float)full / 8) - original_rank == verticaloffsets[j] / 8 && full >= 0 && full < 64)
                        {
                            if (for_white)
                            {
                                attacked_black[full] += piece_values[piecename];
                            }
                            else
                            {
                                attacked_white[full] += piece_values[piecename];
                            }
                            if (!(((pieces[full] & ~7) == 8) == white && (pieces[full] & 7) != 0))
                            {
                                if ((for_white && attacked_white[full] == 0) || (!for_white && attacked_black[full] == 0))
                                {
                                    Move kingmove = new Move();
                                    kingmove.start = i;
                                    kingmove.end = full;
                                    if (white_move) kingmove.castling_before = castlable_white;
                                    else kingmove.castling_before = castlable_black;
                                    kingmove.capture_piece = pieces[kingmove.end] & 7;
                                    moves.Add(kingmove);
                                }
                            }
                        }
                        
                    }
                }
            }
        }
        if (check)
        {
            int i = king_index;
            int attacking_index = -1;
            bool float_check = false;
            List<int> block_squares = new List<int>();
            if (for_white)
            {
                if (Mathf.Abs(i / 8 - (i + 7) / 8) == 1 && i + 7 < 64 && (pieces[i + 7] & 7) == 1 && (pieces[i + 7] & ~7) == 16)
                {
                    attacking_index = i + 7;
                }
                else if (Mathf.Abs(i / 8 - (i + 9) / 8) == 1 && i + 9 < 64 && (pieces[i + 9] & 7) == 1 && (pieces[i + 9] & ~7) == 16)
                {
                    attacking_index = i + 9;
                }
            }
            else
            {
                if (Mathf.Abs(i / 8 - (i - 7) / 8) == 1 && i - 7 >= 0 && (pieces[i - 7] & 7) == 1 && (pieces[i - 7] & ~7) == 32)
                {
                    attacking_index = i - 7;
                }
                else if (Mathf.Abs(i / 8 - (i - 9) / 8) == 1 && i - 9 >= 0 && (pieces[i - 9] & 7) == 1 && (pieces[i - 9] & ~7) == 32)
                {
                    attacking_index = i - 9;
                }
            }
            int[] verticaloffsets = { 8, 8, -8, -8, 16, 16, -16, -16 };
            int[] horizontaloffsets = { 2, -2, 2, -2, 1, -1, 1, -1 };
            for (int j = 0; j < 8; j++)
            {
                int original_rank = i / 8;
                int vertig = i + verticaloffsets[j];
                int full = vertig + horizontaloffsets[j];
                if (Mathf.Floor((float)full / 8) - original_rank == verticaloffsets[j] / 8 && full >= 0 && full < 64)
                {
                    if (((pieces[full] & ~7) == 8) != for_white && (pieces[full] & 7) == 2)
                    {
                        if (attacking_index == -1)
                        {
                            attacking_index = full;
                            block_squares.Add(full);
                        }
                        else
                        {
                            float_check = true;
                        }
                        break;
                    }
                }
            }
            int[] rookoffsets = { 8, -8, 1, -1 };
            int[] bishopoffsets = { 7, 9, -7, -9 };
            for (int t = 0; t < 4; t++)
            {
                int current = i;
                for (int j = 0; j < 8; j++)
                {
                    int og_rank = current / 8;
                    current += rookoffsets[t];
                    if ((Mathf.Abs(rookoffsets[t]) == 1 && current / 8 != og_rank) || current < 0 || current >= 64)
                    {
                        break;
                    }
                    bool currwhite = (pieces[current] & ~7) == 8;
                    if ((pieces[current] & 7) != 0 && currwhite == for_white)
                    {
                        break;
                    }
                    if (((pieces[current] & 7) == 4 || (pieces[current] & 7) == 5) && currwhite != for_white)
                    {
                        if (attacking_index == -1)
                        {
                            attacking_index = current;
                            block_squares.Add(current);
                            for (int q = 1; q <= j; q++)
                            {
                                block_squares.Add(current - q * rookoffsets[t]);
                            }
                        }
                        else
                        {
                            float_check = true;
                        }
                        break;
                    }

                }
                current = i;
                for (int j = 0; j < 8; j++)
                {
                    int currank = current / 8;
                    current += bishopoffsets[t];
                    int newrank = current / 8;
                    if (Mathf.Abs(newrank - currank) != 1 || current < 0 || current >= 64) break;
                    bool currwhite = (pieces[current] & ~7) == 8;
                    if ((pieces[current] & 7) != 0 && currwhite == for_white)
                    {
                        break;
                    }
                    if (((pieces[current] & 7) == 3 || (pieces[current] & 7) == 5) && currwhite != for_white)
                    {
                        if (attacking_index == -1)
                        {
                            attacking_index = current;
                            block_squares.Add(current);
                            for(int q = 1; q <= j; q++)
                            {
                                block_squares.Add(i + q * bishopoffsets[t]);
                            }
                        }
                        else
                        {
                            float_check = true;
                        }
                        break;
                    }

                }
            }
            for (int j = 0; j < moves.Count; j++)
            {
                Move move = moves[j];
                bool move_block = false;
                foreach(int block in block_squares)
                {
                    if(move.end == block && !float_check)
                    {
                        move_block = true;
                    }
                }
                if (move.start != king_index && !move_block)
                {
                    moves.RemoveAt(j);
                    j -= 1;
                }
            }

            //Debug.Log("check by " + attacking_index);
        }

        for(int i = 0; i < moves.Count; i++)
        {
            Move move = moves[i];
            int dst = Mathf.Abs(move.start - move.end);
            if (pinned_pieces[move.start] > 0)
            {
                int pin_dir = pinned_pieces[move.start];
                int type = pieces[move.start] & 7;
                int move_dir = -1;
                int move_distance = move.end - move.start;
                if (move.start / 8 == move.end / 8)
                {
                    move_dir = 4;
                }
                else if (move_distance % 8 == 0)
                {
                    move_dir = 3;
                }
                else if (move_distance % 7 == 0)
                {
                    move_dir = 1;
                }
                else if(move_distance % 9 == 0)
                {
                    move_dir = 2;
                }
                if((pin_dir == 10 && type == 1 && dst != 8 && dst != 16 && (pieces[move.end] & 7) == 0) || (move_dir != pin_dir && pin_dir != 10))
                {
                    moves.RemoveAt(i);
                    i -= 1;
                }
            }
            if ((pieces[move.start] & 7) == 6 && dst == 2 && check)
            {
                moves.RemoveAt(i);
            }
        }
        if(moves.Count == 0)
        {
            if (check)
            {
                if(white_move == for_white)
                {
                    if (for_white) black_win = true;
                    else white_win = true;
                    if(!search) Debug.Log("checkmate");
                }
            }   
            else
            {
                if (white_move == for_white)
                {
                    black_win = true;
                    white_win = true;
                    if(!search) Debug.Log("stalemate");
                }
            }
        }
        return moves;
    }
    bool ForcedWin(bool for_white)
    {
        bool forc = false;
        int their_pieces = 0;
        int my_queens = 0;
        int my_rooks = 0;
        int my_bishops = 0;
        int my_knights = 0;
        int their_rooks = 0;
        int their_knights = 0;
        int their_bishops = 0;
        for(int i = 0; i < 64; i++)
        {
            int piece = pieces[i];
            bool white = (piece & ~7) == 8;
            int piecename = piece & 7;
            if(white == for_white)
            {
                if(piecename == Pieces.Queen)
                {
                    my_queens++;
                }
                else if (piecename == Pieces.Rook)
                {
                    my_rooks++;
                }
                else if (piecename == Pieces.Knight)
                {
                    my_knights++;
                }
                else if (piecename == Pieces.Bishop)
                {
                    my_bishops++;
                }
            }
            else if(piecename != 0)
            {
                their_pieces++;
                if(piecename == Pieces.Rook)
                {
                    their_rooks++;
                }
                else if(piecename == Pieces.Knight)
                {
                    their_knights++;
                }
                else if(piecename == Pieces.Bishop)
                {
                    their_bishops++;
                }
            }
        }
        if (their_pieces == 1)
        {
            if (my_queens > 0 || my_rooks > 0 || my_bishops > 1 || (my_bishops == 1 && my_knights > 0))
            {
                forc = true;
            }
        }
        else if(their_pieces == 2)
        {
            if((their_rooks + their_knights + their_bishops == 1 && my_queens > 0) || (my_rooks > 0 && my_bishops > 0 && their_rooks > 0) || (my_bishops > 1 && their_knights > 0) || (my_rooks > 1 && their_rooks > 0))
            {
                forc = true;
            }
        }
        else if(their_pieces == 3)
        {
            if(my_queens > 0)
            {
                if(their_bishops + their_knights > 1)
                {
                    forc = true;
                }
            }
        }
        return forc;
    }
    private bool white_win = false;
    private bool black_win = false;
    byte[] PiecesToBytes()
    {
        byte[] bytes = new byte[64];
        for(int i = 0; i < 64; i++)
        {
            bytes[i] = (byte)pieces[i];
        }
        return bytes;
    }
    void LoadBytes(byte[] bytes)
    {
        for (int i = 0; i < 64; i++)
        {
            pieces[i] = bytes[i];
        }
    }
    private List<byte[]> last_previous_pieces;
    void Start()
    {
        //run editor in background
        Application.runInBackground = true;
        foreach (GameObject square in highlight_squares)
        {
            square.SetActive(false);
        }
        previous_positions = new List<Position>();
        white_promotion.SetActive(false);
        black_promotion.SetActive(false);
        lastmove = new Move();
        lastmove.start = -1;
        lastmove.end = -1;
        two_moves = new Move();
        two_moves.start = -1;
        two_moves.end = -1;
        attacked_white = new int[64];
        attacked_black = new int[64];
        white_move = true;
        pieces = new int[64];
        pinned_pieces = new int[64];
        last_previous_positions = new List<Position>();
        for(int i = 0; i < 64; i++)
        {
            pieces[i] = Pieces.White | Pieces.None;
            attacked_white[i] = 0;
            attacked_black[i] = 0;
            pinned_pieces[i] = 0;
        }
        FenToPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");
        legalmoves = GenerateMoves(white_move);
        Render();
        //while (GameState() == 0)
        //{
        //yield return null;
        //MovePiece(GenerateMove());
        //}

        //StartCoroutine(Test());
        //StartCoroutine(RunTraining());
        float evaluat = 1 / (1 + Mathf.Pow(10, Evaluate() * 0.25f));
        if (white_move)
        {
            evaluat = 1 - evaluat;
        }
        eval_bar.SetFloat("_progress", evaluat);
        Debug.Log(evaluat);
        if (persist.flipped)
        {
            StartCoroutine(BotMove());
            revvv = !revvv;
            Render();
            Vector3 bly = black_promotion.transform.position;
            black_promotion.transform.position = white_promotion.transform.position;
            white_promotion.transform.position = bly;
            evalbar.transform.Rotate(0, 0, 180);
        }
    }
    int currentind = 0;
    Vector3 ogpos;
    private Move lastmove;
    private bool indinrange = false;
    private bool search = false;
    private int promo_capture = 0;
    bool revvv = false;
    void Update()
    {
        
        Vector3 worldpos = maincamera.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            currentind = (int)(Mathf.Round(worldpos.y + 3.5f) * 8 + Mathf.Round(worldpos.x + 3.5f));
            if (worldpos.y < -4 || worldpos.y > 4 || worldpos.x < -4 || worldpos.x > 4) indinrange = false;
            else indinrange = true;
            if (indinrange)
            {
                ogpos = renderers[currentind].transform.position;
                renderers[currentind].GetComponent<SpriteRenderer>().sortingLayerName = "top";
            }

        }
        if (Input.GetMouseButton(0))
        {
            worldpos.z = 0;
            if (indinrange) renderers[currentind].transform.position = worldpos;
        }
        if (Input.GetMouseButtonUp(0) && indinrange)
        {
            renderers[currentind].transform.position = ogpos;
            int newind = (int)(Mathf.Round(worldpos.y + 3.5f) * 8 + Mathf.Round(worldpos.x + 3.5f));
            if (revvv)
                newind = 63 - newind;

            if (revvv)
                currentind = 63 - currentind;
            bool legal = false;
            Move tsmove = new Move();
            foreach (Move legalmove in legalmoves)
            {
                if (legalmove.start == currentind && legalmove.end == newind)
                {
                    legal = true;
                    tsmove = legalmove;
                }

            }
            renderers[tsmove.start].GetComponent<SpriteRenderer>().sortingLayerName = "pieces";
            if (legal && !promoting)
            {
                if (tsmove.promotion_piece != 0)
                {
                    tsmove.promotion_piece = 1;
                    if (white_move)
                    {
                        white_promotion.SetActive(true);
                    }
                    else
                    {
                        black_promotion.SetActive(true);
                    }
                    promoting = true;
                    promo_capture = pieces[tsmove.end] & 7;
                    pieces[tsmove.end] = pieces[tsmove.start];
                    pieces[tsmove.start] = Pieces.White | Pieces.None;
                    promotion_square = tsmove.end;
                    promotion_start = tsmove.start;
                    Render();
                }
                else
                {
                    MovePiece(tsmove);
                    //MovePiece(legalmoves[UnityEngine.Random.Range(0, legalmoves.Count)]);

                }
            }

        }
    }
    void MovePieceSimple(Move tsmove)
    {
        two_moves = lastmove;
        lastmove = tsmove;
        int cur = pieces[tsmove.start];
        int dist = (int)Mathf.Abs(tsmove.end - tsmove.start);
        if ((cur & 7) == 1 && (pieces[tsmove.end] & 7) == 0 && dist != 16 && dist != 8)
        {
            //en passant
            if (white_move)
            {
                pieces[tsmove.end - 8] = Pieces.White | Pieces.None;
            }
            else
            {
                pieces[tsmove.end + 8] = Pieces.White | Pieces.None;
            }
        }
        if ((cur & 7) == 6 && dist == 2)
        {
            //castling
            int cur_place = tsmove.start;
            int offs = (int)((tsmove.end - tsmove.start) * 0.5f);
            cur_place += offs * 2;
            for (int i = 0; i < 2; i++)
            {
                cur_place += offs;
                if ((pieces[cur_place] & 7) == 4)
                {
                    int cur_piece = pieces[cur_place];
                    pieces[cur_place] = Pieces.White | Pieces.None;
                    pieces[tsmove.start + offs] = cur_piece;
                    break;
                }
            }
        }
        if (tsmove.promotion_piece > 0)
        {
            cur = (cur & ~7) | tsmove.promotion_piece;
        }
        pieces[tsmove.end] = cur;
        pieces[tsmove.start] = Pieces.White | Pieces.None;
        white_move = !white_move;
    }
    void MovePiece(Move tsmove)
    {
        two_moves = lastmove;
        lastmove = tsmove;
        int cur = pieces[tsmove.start];
        if (reset_next)
        {
            previous_positions.Clear();
            reset_next = false;
        }
        if ((cur & 7) == 1 || (pieces[tsmove.end] & 7) != 0)
        {
            reset_next = true;
        }
        if((cur & 7) == 6 || (cur & 7) == 4)
        {
            if (white_move) castlable_white = false;
            else castlable_black = false;
        }
        int dist = (int)Mathf.Abs(tsmove.end - tsmove.start);
        if ((cur & 7) == 1 && (pieces[tsmove.end] & 7) == 0 && dist != 16 && dist != 8)
        {
            //en passant
            if (white_move)
            {
                pieces[tsmove.end - 8] = Pieces.White | Pieces.None;
            }
            else
            {
                pieces[tsmove.end + 8] = Pieces.White | Pieces.None;
            }
        }
        if ((cur & 7) == 6 && dist == 2)
        {
            //castling
            int cur_place = tsmove.start;
            int offs = (int)((tsmove.end - tsmove.start) * 0.5f);
            cur_place += offs * 2;
            for(int i = 0; i < 2; i++)
            {
                cur_place += offs;
                if ((pieces[cur_place] & 7) == 4)
                {
                    int cur_piece = pieces[cur_place];
                    pieces[cur_place] = Pieces.White | Pieces.None;
                    pieces[tsmove.start + offs] = cur_piece;
                    break;
                }
            }
        }
        if(tsmove.promotion_piece > 0)
        {
            cur = (cur & ~7) | tsmove.promotion_piece;
        }
        pieces[tsmove.end] = cur;
        pieces[tsmove.start] = Pieces.White | Pieces.None;

        white_move = !white_move;
        if (!search)
        {
            Render();

        }
        List<Move> trash = GenerateMoves(!white_move);
        trashcount = trash.Count;
        legalmoves = GenerateMoves(white_move);
        Position this_position = StorePosition();
        previous_positions.Add(this_position);
        if (previous_positions.Count >= 50 && !reset_next)
        {
            white_win = true;
            black_win = true;
            if(!search)Debug.Log("draw by 50 move rule");
        }
        bool other_piece = false;
        int black_bishops = 0;
        int black_knights = 0;
        int white_knights = 0;
        int white_bishops = 0;
        for(int i = 0; i < 64; i++)
        {
            int piece_name = pieces[i] & 7;
            if (piece_name == 2)
            {
                if ((pieces[i] & ~7) == 8)
                {
                    white_knights += 1;
                }
                else
                {
                    black_knights += 1;
                }
            }
            else if(piece_name == 3)
            {
                if ((pieces[i] & ~7) == 8)
                {
                    white_bishops += 1;
                }
                else
                {
                    black_bishops += 1;
                }
            }
            else if(piece_name != 6 && piece_name != 0)
            {
                other_piece = true;
                break;
            }
        }
        if (!other_piece)
        {
            bool white_case = false;
            bool black_case = false;
            if(black_bishops + black_knights < 2)
            {
                black_case = true;
            }
            if (white_bishops + white_knights < 2)
            {
                white_case = true;
            }
            if((black_case && white_case) || (white_bishops + black_bishops == 0 && black_knights + white_knights == 2))
            {
                white_win = true;
                black_win = true;
                if (!search)
                {
                    Debug.Log("insufficient material");
                }
            }
        }
        int repetitions = 0;
        foreach (Position pos in previous_positions)
        {
            if (pos.Compare(this_position))
            {
                    repetitions++;
            }
        }
        if (repetitions >= 3 && !reset_next)
        {
            white_win = true;
            black_win = true;
            if (!search) Debug.Log("draw by 3 fold repetition");
        }
        if (!search)
        {
            float evaluat = 1 / (1 + Mathf.Pow(10, Evaluate() * 0.25f));
            if (white_move)
            {
                evaluat = 1 - evaluat;
            }
            eval_bar.SetFloat("_progress", evaluat);
            Debug.Log(evaluat);
        }
        if(!search && (persist.flipped == white_move))
        {
            StartCoroutine(BotMove());
        }
        if (!search)
        {
            for(int i = 0; i < 64; i++)
            {
                highlight_squares[i].SetActive(false);
            }
            if (persist.flipped)
            {
                highlight_squares[63 - tsmove.start].SetActive(true);
                highlight_squares[63 - tsmove.end].SetActive(true);
            }
            else
            {
                highlight_squares[tsmove.start].SetActive(true);
                highlight_squares[tsmove.end].SetActive(true);
            }
        }
    }
    IEnumerator BotMove()
    {
        yield return null;
        MovePiece(GenerateMove());
    }
    void UnMovePieceSimple(Move tsmove)
    {
        white_move = !white_move;
        lastmove = two_moves;
        int cur = pieces[tsmove.end];
        int dist = (int)Mathf.Abs(tsmove.end - tsmove.start);
        if ((cur & 7) == 1 && tsmove.capture_piece == 0 && dist != 16 && dist != 8)
        {
            //en passant
            if (white_move)
            {
                pieces[tsmove.end - 8] = Pieces.Black | Pieces.Pawn;
            }
            else
            {
                pieces[tsmove.end + 8] = Pieces.White | Pieces.Pawn;
            }
        }
        if ((cur & 7) == 6 && dist == 2)
        {
            //castling
            if (tsmove.end > tsmove.start)
            {
                pieces[tsmove.start + 3] = pieces[tsmove.start + 1];
                pieces[tsmove.start + 1] = Pieces.White | Pieces.None;
            }
            else
            {
                pieces[tsmove.start - 4] = pieces[tsmove.start - 1];
                pieces[tsmove.start - 1] = Pieces.White | Pieces.None;
            }
        }
        if (tsmove.promotion_piece > 0)
        {
            cur = (cur & ~7) | Pieces.Pawn;
        }
        pieces[tsmove.start] = cur;
        if (white_move)
        {
            pieces[tsmove.end] = tsmove.capture_piece | Pieces.Black;
        }
        else
        {
            pieces[tsmove.end] = tsmove.capture_piece | Pieces.White;
        }
    }
    private bool reset_next;
    void UnMovePiece(Move tsmove)
    {
        lastmove = two_moves;
        reset_next = false;
        white_move = !white_move;
        int cur = pieces[tsmove.end];
        if (white_move)
        {
            castlable_white = tsmove.castling_before;
        }
        else
        {
            castlable_black = tsmove.castling_before;
        }
        int dist = (int)Mathf.Abs(tsmove.end - tsmove.start);
        if ((cur & 7) == 1 && tsmove.capture_piece == 0 && dist != 16 && dist != 8)
        {
            //en passant
            if (white_move)
            {
                pieces[tsmove.end - 8] = Pieces.Black | Pieces.Pawn;
            }
            else
            {
                pieces[tsmove.end + 8] = Pieces.White | Pieces.Pawn;
            }
        }
        if ((cur & 7) == 6 && dist == 2)
        {
            //castling
            if(tsmove.end > tsmove.start)
            {
                pieces[tsmove.start + 3] = pieces[tsmove.start + 1];
                pieces[tsmove.start + 1] = Pieces.White | Pieces.None;
            }
            else
            {
                pieces[tsmove.start - 4] = pieces[tsmove.start - 1];
                pieces[tsmove.start - 1] = Pieces.White | Pieces.None;
            }
        }
        if (tsmove.promotion_piece > 0)
        {
            cur = (cur & ~7) | Pieces.Pawn;
        }
        pieces[tsmove.start] = cur;
        if (white_move)
        {
            pieces[tsmove.end] = tsmove.capture_piece | Pieces.Black;
        }
        else
        {
            pieces[tsmove.end] = tsmove.capture_piece | Pieces.White;
        }
        if (!search)
        {
            Render();
        }
        List<Move> trash = GenerateMoves(!white_move);
        legalmoves = GenerateMoves(white_move);
        if(previous_positions.Count > 0)
        {
            previous_positions.RemoveAt(previous_positions.Count - 1);
        }
        white_win = false;
        black_win = false;
    }
    void FenToPosition(string fen)
    {
        int current_square = 56;
        foreach(char c in fen)
        {
            if (c == 'p')
            {
                pieces[current_square] = Pieces.Black | Pieces.Pawn;
                current_square += 1;
            }
            else if (c == 'n')
            {
                pieces[current_square] = Pieces.Black | Pieces.Knight;
                current_square += 1;
            }
            else if (c == 'b')
            {
                pieces[current_square] = Pieces.Black | Pieces.Bishop;
                current_square += 1;
            }
            else if (c == 'r')
            {
                pieces[current_square] = Pieces.Black | Pieces.Rook;
                current_square += 1;
            }
            else if (c == 'q')
            {
                pieces[current_square] = Pieces.Black | Pieces.Queen;
                current_square += 1;
            }
            else if (c == 'k')
            {
                pieces[current_square] = Pieces.Black | Pieces.King;
                current_square += 1;
            }
            else if (c == 'P')
            {
                pieces[current_square] = Pieces.White | Pieces.Pawn;
                current_square += 1;
            }
            else if (c == 'N')
            {
                pieces[current_square] = Pieces.White | Pieces.Knight;
                current_square += 1;
            }
            else if (c == 'B')
            {
                pieces[current_square] = Pieces.White | Pieces.Bishop;
                current_square += 1;
            }
            else if (c == 'R')
            {
                pieces[current_square] = Pieces.White | Pieces.Rook;
                current_square += 1;
            }
            else if (c == 'Q')
            {
                pieces[current_square] = Pieces.White | Pieces.Queen;
                current_square += 1;
            }
            else if (c == 'K')
            {
                pieces[current_square] = Pieces.White | Pieces.King;
                current_square += 1;
            }
            else if (c == '/')
            {
                current_square -= 16;
            }
            else if (Char.IsDigit(c))
            {
                for(int j = 0; j < (c - '0'); j++)
                {
                    pieces[current_square] = Pieces.White | Pieces.None;
                    current_square += 1;
                }
            }
        }
    }
    void Render()
    {
        for(int i = 0; i < 64; i++)
        {
            int piece = 0;
            if (!revvv)
                piece = pieces[i];
            else
                piece = pieces[63 - i];
            int colour = piece & ~7;
            int piecename = piece & 7;
            if(colour == 8)
            {
                renderers[i].GetComponent<SpriteRenderer>().sprite = whitesprites[piecename];
            }
            else
            {
                renderers[i].GetComponent<SpriteRenderer>().sprite = blacksprites[piecename];
            }
        }
    }
}