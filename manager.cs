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
        public int wins;
        public PositionNode()
        {
            visits = 0;
            wins = 0;
        }
    }
    public class Layer
    {
        public int nodesin;
        public int nodesout;
        public float[,] weights;
        public float[] biases;
        public float[] weightedinputs;
        public float[] thisinputs;
        public float[,] weightgradients;
        public float[] biasgradients;
        public float[] outputs;
        public Layer(int nodesin, int nodesout)
        {
            this.nodesin = nodesin;
            this.nodesout = nodesout;
            weights = new float[nodesin, nodesout];
            biases = new float[nodesout];
            outputs = new float[nodesout];
            weightedinputs = new float[nodesout];
            weightgradients = new float[nodesin, nodesout];
            biasgradients = new float[nodesout];
            thisinputs = new float[nodesin];
        }
        public float[] CalculateOutputs(float[] inputs, bool hiddenlayer, bool traceable)
        {
            if(traceable) thisinputs = inputs;
            for (int i = 0; i < nodesout; i++)
            {
                float thisinput = biases[i];
                for (int j = 0; j < nodesin; j++)
                {
                    thisinput += inputs[j] * weights[j, i];
                }
                if(traceable)weightedinputs[i] = thisinput;
                if (hiddenlayer)
                {
                    thisinput = Activation(thisinput);
                }
                else
                {
                    thisinput = ActivationSigmoid(thisinput);
                }
                outputs[i] = thisinput;
            }
            return outputs;
        }
        public float[] CalculateOutputsNoSigmoid(float[] inputs, bool hiddenlayer, bool traceable)
        {
            if (traceable) thisinputs = inputs;
            for (int i = 0; i < nodesout; i++)
            {
                float thisinput = biases[i];
                for (int j = 0; j < nodesin; j++)
                {
                    thisinput += inputs[j] * weights[j, i];
                }
                if (traceable) weightedinputs[i] = thisinput;
                if (hiddenlayer)
                {
                    thisinput = Activation(thisinput);
                }
                outputs[i] = thisinput;
            }
            return outputs;
        }
        public float[] CalculateOutputsAccumulator(float[] inputs, float[] accumulator, bool traceable)
        {
            if(traceable) thisinputs = inputs;
            for (int i = 0; i < nodesout; i++)
            {
                outputs[i] = Activation(accumulator[i]);
                if(traceable) weightedinputs[i] = accumulator[i];
            }
            return outputs;
        }

        public void ClearGradients()
        {
            for (int i = 0; i < nodesout; i++)
            {
                biasgradients[i] = 0;
                for (int j = 0; j < nodesin; j++)
                {
                    weightgradients[j, i] = 0;
                }
            }

        }
        public void ApplyGradients(float multiple)
        {
            for (int i = 0; i < nodesout; i++)
            {
                biases[i] -= multiple * biasgradients[i];
                //Debug.Log(biasgradients[i]);
                for (int j = 0; j < nodesin; j++)
                {
                    weights[j, i] -= multiple * weightgradients[j, i];
                    //Debug.Log(weightgradients[j,i]);
                }
            }
        }
        public void UpdateGradients(float[] values)
        {
            for (int i = 0; i < nodesout; i++)
            {
                for (int j = 0; j < nodesin; j++)
                {
                    float derweight = thisinputs[j] * values[i];
                    weightgradients[j, i] += derweight;
                }
                float derbias = values[i];
                biasgradients[i] += derbias;
            }
        }
        public float[] OutputValues(float[] expected)
        {
            float[] nodevalues = new float[expected.Length];
            for (int i = 0; i < expected.Length; i++)
            {
                float errornotsquared = outputs[i] - expected[i];
                nodevalues[i] = 2 * errornotsquared * SigmoidDerivative(weightedinputs[i]);
            }
            return nodevalues;
        }
        public float[] HiddenNodeValues(Layer old, float[] oldvalues)
        {
            float[] newvalues = new float[nodesout];
            for (int newnode = 0; newnode < nodesout; newnode++)
            {
                float newnodevalue = 0;
                for (int oldnodeind = 0; oldnodeind < oldvalues.Length; oldnodeind++)
                {
                    float weightinputder = old.weights[newnode, oldnodeind];
                    newnodevalue += weightinputder * oldvalues[oldnodeind];
                }
                newnodevalue *= ActivationDerivative(weightedinputs[newnode]);
                newvalues[newnode] = newnodevalue;
            }
            return newvalues;
        }
        public float Activation(float input)
        {
            //float oneplusexp = 1 + Math.Exp(-input);
            //return 1 / oneplusexp;
            return Mathf.Clamp01(input);
        }
        public float ActivationDerivative(float input)
        {
            //float oneplusexp = 1 + Math.Exp(-input);
            //float sigmoid = 1 / oneplusexp;
            //return sigmoid * (1 - sigmoid);
            if (input > 0 && input < 1)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
        public float ActivationSigmoid(float input)
        {
            float oneplusexp = 1 + Mathf.Exp(-input);
            return 1 / oneplusexp;
        }
        public float SigmoidDerivative(float input)
        {
            float oneplusexp = 1 + Mathf.Exp(-input);
            float sigmoid = 1 / oneplusexp;
            return sigmoid * (1 - sigmoid);
        }
    }
    public struct Datapoint
    {
        public float[] inputs;
        public float[] outputs;
    }
    private int[][] nodesnumbers;
    [SerializeField]private int[] nodes1;
    [SerializeField]private int[] nodes2;
    [SerializeField]private int[] nodes3;
    [SerializeField] private float learnrate;
    private float[][] accumulators;
    private Layer[][] networks;
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
    private void Refresh_Accumulators(float[] inputs)
    {
        for (int t = 0; t < networks.Length; t++)
        {
            Layer layer = networks[t][0];
            for (int i = 0; i < layer.nodesout; i++)
            {
                float thisinput = layer.biases[i];
                for (int j = 0; j < layer.nodesin; j++)
                {
                    if (inputs[j] != 0)
                    {
                        thisinput += inputs[j] * layer.weights[j, i];
                    }
                }
                accumulators[t][i] = thisinput;
            }
        }
    }
    private void Update_Accumulators(int index, bool add)
    {
        for(int j = 0; j < networks.Length; j++)
        {
            Layer layer = networks[j][0];
            for (int i = 0; i < layer.nodesout; i++)
            {
                if (add)
                {
                    accumulators[j][i] += layer.weights[index, i];
                }
                else
                {
                    accumulators[j][i] -= layer.weights[index, i];
                }
            }
        }
    }
    private byte[] WeightsToByte()
    {
        List<float> floats = new List<float>();
        for(int i = 0; i < networks.Length; i++)
        {
            for(int j = 0; j < networks[i].Length; j++)
            {
                for(int t = 0; t < networks[i][j].nodesout; t++)
                {
                    for(int p = 0; p < networks[i][j].nodesin; p++)
                    {
                        floats.Add(networks[i][j].weights[p, t]);
                    }
                    floats.Add(networks[i][j].biases[t]);
                }
            }
        }
        byte[] bytes = new byte[floats.Count * 4];
        Buffer.BlockCopy(floats.ToArray(), 0, bytes, 0, bytes.Length);
        return bytes;
    }
    private void ByteToWeights(byte[] bytes)
    {
        float[] floats = new float[bytes.Length / 4];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        int float_ind = 0;
        for (int i = 0; i < networks.Length; i++)
        {
            for (int j = 0; j < networks[i].Length; j++)
            {
                for (int t = 0; t < networks[i][j].nodesout; t++)
                {
                    for (int p = 0; p < networks[i][j].nodesin; p++)
                    {
                        networks[i][j].weights[p, t] = floats[float_ind];
                        float_ind++;
                    }
                    networks[i][j].biases[t] = floats[float_ind];
                    float_ind++;
                }
            }
        }
    }
    private float[] Calculate(float[] inputs, int network_index)
    {
        Layer[] layers = networks[network_index];
        float[] previousoutput = layers[0].CalculateOutputsAccumulator(inputs, accumulators[network_index], false);
        for (int i = 1; i < layers.Length; i++)
        {
            previousoutput = layers[i].CalculateOutputs(previousoutput, i < layers.Length - 1, false);
        }
        return previousoutput;
    }
    private float[] CalculateNoSigmoid(float[] inputs, int network_index)
    {
        Layer[] layers = networks[network_index];
        float[] previousoutput = layers[0].CalculateOutputsAccumulator(inputs, accumulators[network_index], false);
        for (int i = 1; i < layers.Length; i++)
        {
            previousoutput = layers[i].CalculateOutputsNoSigmoid(previousoutput, i < layers.Length - 1, false);
        }
        return previousoutput;
    }
    private float[] Run(float[] inputs, int network_index)
    {
        Layer[] layers = networks[network_index];
        float[] previousoutput = layers[0].CalculateOutputsAccumulator(inputs, accumulators[network_index], true);
        for (int i = 1; i < layers.Length; i++)
        {
            previousoutput = layers[i].CalculateOutputs(previousoutput, i < layers.Length - 1, true);
        }
        return previousoutput;
    }
    //one training step
    private void NeuralNetwork(Datapoint[] datapoints, int network)
    {
        Layer[] layers = networks[network];
        foreach (Layer layer in layers)
        {
            layer.ClearGradients();
        }
        float mse = 0;
        foreach (Datapoint datapoint in datapoints)
        {
            mse += Backpropagation(datapoint.inputs, datapoint.outputs, network);

        }
        mse /= datapoints.Length;
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i].ApplyGradients(learnrate / datapoints.Length);

        }
    }
    private float Backpropagation(float[] datapointinput, float[] datapointoutput, int network)
    {
        Layer[] layers = networks[network];
        float[] output = Run(datapointinput, network);
        float se = 0;
        for(int i  = 0; i < output.Length; i++)
        {
            float error = output[i] - datapointoutput[i];
            se += error * error;
        }
        Layer lastlayer = layers[layers.Length - 1];
        float[] nodevalues = lastlayer.OutputValues(datapointoutput);
        lastlayer.UpdateGradients(nodevalues);
        for (int hidd = layers.Length - 2; hidd >= 0; hidd--)
        {
            Layer hiddenlayer = layers[hidd];
            Layer oldlayer = layers[hidd + 1];
            nodevalues = hiddenlayer.HiddenNodeValues(oldlayer, nodevalues);
            hiddenlayer.UpdateGradients(nodevalues);
        }
        return se;
    }
    float Evaluate()
    {
        float eval = 0;
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
            int ee = piecewhite ? i : 63 - i;
                if (piecenum == 1)
                {
                    plv += Mathf.Lerp(pawntable[ee], pawntable2[ee], taper) * 0.5f;
                }
                else if (piecenum == Pieces.Rook)
                {
                    plv += Mathf.Lerp(rooktable[ee], rooktable2[ee], taper) * 0.5f;
                }
                else if (piecenum == Pieces.Bishop)
                {
                    plv += Mathf.Lerp(bishoptable[ee], bishoptable2[ee], taper) * 0.5f;
                }
                else if(piecenum == Pieces.Knight)
                {
                    plv += Mathf.Lerp(knighttable[ee], knighttable2[ee], taper) * 0.5f;
                }
                else if(piecenum == 6)
                {
                    plv += Mathf.Lerp(kingtable[ee], kingtable2[ee], taper) * 0.5f;
                }
            if (pinned_pieces[i] != 0)
            {
                if (white_move)
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
                if (attacked_white[i] > 0 && attacked_black[i] < 2)
                {
                    //Debug.Log("holy undefended on " + i);
                    plv = plv * 0.1f;
                }
                eval += plv;
            }
            else
            {
                if (attacked_black[i] > 0 && attacked_white[i] < 2)
                {
                   // Debug.Log("holy undefended on " + i);
                    plv = plv * 0.1f;
                }
                eval -= plv;
            }

        }
        if (!white_move)
        {
            eval = -eval;
        }
        return eval;
    }
    float[] GetInputs(bool update_accum)
    {
        float[] inputs = new float[898];
        for(int i = 0; i < 898; i++)
        {
            inputs[i] = 0;
        }
        for(int i = 0; i < 64; i++)
        {
            int start_index = i * 14;
            int this_i;
            if (white_move)
            {
                this_i = i;
            }
            else
            {
                this_i = 63 - i;
            }
            int piece_name = pieces[this_i] & 7;
            bool piece_colour = ((pieces[this_i] & ~7) == 8) == white_move;
            if(piece_name == 1 && lastmove.end == this_i && lastmove.passant_target == true)
            {
                piece_name = 7;
            }
            if(piece_name != 0)
            {
                if (piece_colour)
                {
                    inputs[start_index - 1 + piece_name] = 1;
                }
                else
                {
                    inputs[start_index + 6 + piece_name] = 1;
                }
            }
        }
        if (white_move)
        {

            inputs[896] = castlable_white ? 1 : 0;
            inputs[897] = castlable_black ? 1 : 0;
        }
        else
        {
            inputs[896] = castlable_black ? 1 : 0;
            inputs[897] = castlable_white ? 1 : 0;
        }
        if (update_accum)
        {
            for (int i = 0; i < 898; i++)
            {
                if (inputs[i] != nnue_inputs[i])
                {
                    if (inputs[i] < nnue_inputs[i])
                    {
                        Update_Accumulators(i, false);
                    }
                    else
                    {
                        Update_Accumulators(i, true);
                    }
                }
            }
        }
        return inputs;
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
    bool insuf_material = false;
    IEnumerator Train_Random()
    {
        search = true;
        int moves = 10000;
        //int moves = UnityEngine.Random.Range(1, 300);
        if(UnityEngine.Random.value < 0.6f)
        {
            moves = UnityEngine.Random.Range(1, 300);
        }
        insuf_material = false;
        for(int i = 0; i < moves; i++)
        {
            if(!white_win && !black_win)
            {
                MovePiece(legalmoves[UnityEngine.Random.Range(0, legalmoves.Count)]);
            }
            else
            {
                break;
            }
        }
        nnue_inputs = GetInputs(true);
        Render();
        if (!white_win && !black_win)
        {
            int games_score = 0;
            PositionNode node = new PositionNode();
            last_position = StorePosition();
            last_previous_positions.Clear();
            last_previous_positions.AddRange(previous_positions);
            node.visits += 1;
            node.children = new PositionNode[legalmoves.Count];
            PositionNode current = node;
            for (int i = 0; i < samples; i++)
            {
                int game_length = 0;
                while (!white_win && !black_win)
                {
                    nnue_inputs = GetInputs(true);
                    game_length++;
                    float[] weights = new float[legalmoves.Count];
                    float total_weight = 0;
                    bool curr_evals = current.evaluations != null;
                    if (!curr_evals)
                    {
                        current.evaluations = new float[legalmoves.Count];
                    }
                    float current_eval = 0;
                    if (white_move == last_position.white_move)
                    {
                        //lastposition white move is reversed but thise white move is not
                        current_eval = Mathf.Clamp(CalculateNoSigmoid(nnue_inputs, 0)[0], -25, 25);
                        //use less smart network
                    }
                    else
                    {
                        current_eval = Mathf.Clamp(CalculateNoSigmoid(nnue_inputs, 1)[0], -25, 25);
                        //use better network
                    }
                    for (int t = 0; t < legalmoves.Count; t++)
                    {
                        Move move = legalmoves[t];
                        MovePieceSimple(move);
                        nnue_inputs = GetInputs(true);
                        float cur_visits = 1;
                        if (current.children[t] != null)
                        {
                            cur_visits = current.children[t].visits;
                        }
                        float node_evaluation = 0;
                        if (curr_evals)
                        {
                            node_evaluation = current.evaluations[t];
                        }
                        else
                        {
                            if (white_move != last_position.white_move)
                            {
                                node_evaluation = Mathf.Clamp(CalculateNoSigmoid(nnue_inputs, 0)[0], -25, 25);
                                //use less smart network
                            }
                            else
                            {
                                node_evaluation = Mathf.Clamp(CalculateNoSigmoid(nnue_inputs, 1)[0], -25, 25);
                                //use better network
                            }
                            //node evaluation is for the side to move next, a pure evaluation without sigmoid scaling
                            current.evaluations[t] = node_evaluation;
                        }
                        //node evaluation is actually opposite to current evaluation
                        weights[t] = 1 / cur_visits * Mathf.Exp((-current_eval - node_evaluation) * inv_temp);
                        UnMovePieceSimple(move);
                        total_weight += weights[t];
                    }
                    float rnd_seed = UnityEngine.Random.Range(0, total_weight);
                    float accum = 0;
                    for (int t = 0; t < legalmoves.Count; t++)
                    {
                        if ((rnd_seed >= accum && rnd_seed < accum + weights[t]) || rnd_seed == total_weight)
                        {
                            if (current.children[t] == null)
                            {
                                current.children[t] = new PositionNode();
                            }
                            current = current.children[t];
                            current.visits += 1;
                            MovePiece(legalmoves[t]);
                            if (current.children == null)
                            {
                                current.children = new PositionNode[legalmoves.Count];
                            }
                            break;
                        }
                        accum += weights[t];
                    }
                }
                yield return null;
                if (!last_position.white_move)
                {
                    if (white_win && !black_win)
                    {
                        games_score += 1;
                    }
                    else if (black_win && !white_win)
                    {
                        games_score -= 1;
                    }
                }
                else
                {
                    if (white_win && !black_win)
                    {
                        games_score -= 1;
                    }
                    else if (black_win && !white_win)
                    {
                        games_score += 1;
                    }
                }
                //if(black_win && white_win)
                //{
                //  float val_eval = ValueEvaluation();
                //   if(val_eval != 0)
                //   {
                //      bool white_more_black = ValueEvaluation() > 0;
                //     if (last_position.white_move)
                //     {
                //        if (white_more_black) games_score += 1;
                //         else games_score -= 1;
                //      }
                //      else
                //       {
                //             if (white_more_black) games_score -= 1;
                //           else games_score += 1;
                //     }
                //          }
                //     }
                previous_positions.Clear();
                previous_positions.AddRange(last_previous_positions);
                current = node;
                white_win = false;
                black_win = false;
                GetPosition(last_position);
                List<Move> lst_move = GenerateMoves(!white_move);
                legalmoves = GenerateMoves(white_move);
                nnue_inputs = GetInputs(true);
            }
            //maximum score
            //Debug.Log(legalmove.start + " to " + legalmove.end + " : " + (float)games_score / (float)samples);
            GetPosition(last_position);
            previous_positions.Clear();
            previous_positions.AddRange(last_previous_positions);
            nnue_inputs = GetInputs(true);
            
            Datapoint datapoint = new Datapoint();
            datapoint.inputs = nnue_inputs;
            datapoint.outputs = new float[1] { (samples + games_score) / (float)(samples * 2) };
            datapoints_list.Add(datapoint);
            SaveDatapoints();
        }
        else if(!black_win || !white_win)
        {
            nnue_inputs = GetInputs(true);
            Datapoint datapoint = new Datapoint();
            datapoint.inputs = nnue_inputs;
            datapoint.outputs = new float[1] { 0.00001f };
            datapoints_list.Add(datapoint);
            black_win = false;
            white_win = false;
            SaveDatapoints();
            //Debug.Log("checkmate");
        }
        else if(insuf_material)
        {
            nnue_inputs = GetInputs(true);
            Datapoint datapoint = new Datapoint();
            datapoint.inputs = nnue_inputs;
            datapoint.outputs = new float[1] { 0.5f };
            datapoints_list.Add(datapoint);
            black_win = false;
            white_win = false;
            SaveDatapoints();
        }
        else
        {
            black_win = false;
            white_win = false;
        }
        FenToPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");
        white_move = true;
        castlable_white = true;
        castlable_black = true;
        previous_positions.Clear();
        for (int i = 0; i < 64; i++)
        {
            attacked_white[i] = 0;
            attacked_black[i] = 0;
            pinned_pieces[i] = 0;
        }
        legalmoves = GenerateMoves(true);

        search = false;
        System.GC.Collect();
    }
    List<Datapoint> datapoints_list;
    void SaveDatapoints()
    {
        float[] input_floats = new float[datapoints_list[0].inputs.Length * datapoints_list.Count];
        float[] output_floats = new float[datapoints_list.Count];
        for(int i = 0; i < datapoints_list.Count; i++)
        {
            Datapoint dat = datapoints_list[i];
            for(int j = 0; j < dat.inputs.Length; j++)
            {
                input_floats[i * datapoints_list[0].inputs.Length + j] = dat.inputs[j];
            }
            output_floats[i] = dat.outputs[0];
        }
        byte[] input_bytes = File.ReadAllBytes("Assets/prev_inputs");
        byte[] output_bytes = File.ReadAllBytes("Assets/prev_outputs");
        byte[] input_full = new byte[input_floats.Length*4 + input_bytes.Length]; 
        byte[] output_full = new byte[output_floats.Length * 4 + output_bytes.Length];
        Buffer.BlockCopy(input_bytes, 0, input_full, input_floats.Length * 4, input_bytes.Length);
        Buffer.BlockCopy(input_floats, 0, input_full, 0, input_floats.Length * 4);
        Buffer.BlockCopy(output_bytes, 0, output_full, output_floats.Length * 4, output_bytes.Length);
        Buffer.BlockCopy(output_floats, 0, output_full, 0, output_floats.Length * 4);
        File.WriteAllBytes("Assets/prev_inputs", input_full);
        File.WriteAllBytes("Assets/prev_outputs", output_full);
    }
    void LoadDatapoints()
    {
        byte[] input_bytes = File.ReadAllBytes("Assets/prev_inputs");
        byte[] output_bytes = File.ReadAllBytes("Assets/prev_outputs");
        float[] input_floats = new float[input_bytes.Length / 4];
        float[] output_floats = new float[output_bytes.Length / 4];
        Buffer.BlockCopy(input_bytes, 0, input_floats, 0, input_bytes.Length);
        Buffer.BlockCopy(output_bytes, 0, output_floats, 0, output_bytes.Length);
        for(int i = 0; i < output_floats.Length; i++)
        {
            Datapoint dat = new Datapoint();
            dat.inputs = new float[input_floats.Length / output_floats.Length];
            dat.outputs = new float[1] { output_floats[i] };
            Array.Copy(input_floats, i * dat.inputs.Length, dat.inputs, 0, dat.inputs.Length);
            datapoints_list.Add(dat);
        }
    }
    const float c_val = 1f;
    const int samples = 20;
    const float noise_magn = 1f;
    Move GenerateMove()
    {
        bool this_forced = ForcedWin(white_move);
        //Debug.Log("search start");
        float max_score = -1;
        float[] games_scores = new float[legalmoves.Count];
        List<Move> max_moves = new List<Move>();
        search = true;
        float tim = Time.realtimeSinceStartup;
        int actual_mates = 0;
        nnue_inputs = GetInputs(true);
        //Debug.Log(Time.realtimeSinceStartup - tim);
        for (int j = 0; j < legalmoves.Count; j++)
        {
            bool debug_this = false;
            Move legalmove = legalmoves[j];
            float games_score = 0;
            
            MovePiece(legalmove);
            last_position = StorePosition();
            last_previous_positions.Clear();
            last_previous_positions.AddRange(previous_positions);
            PositionNode node = new PositionNode();
            node.visits += 1;
            node.children = new PositionNode[legalmoves.Count];
            PositionNode current = node;
            int this_samples = samples;
            float last_rand = -1;
            for (int i = 0; i < this_samples; i++)
            {
                node.visits += 1;
                int game_length = 0;
                List<int> prev_nodes = new List<int>();
                int last_visits = node.visits;
                while (!white_win && !black_win)
                {
                    nnue_inputs = GetInputs(true);
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
                        MovePieceSimple(move);
                        float cur_visits = 1;
                        float win_probability = 0;
                        if (current.children[t] != null)
                        {
                           cur_visits = current.children[t].visits;
                           win_probability = (cur_visits + current.children[t].wins) / (float)(cur_visits * 2);
                        }
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
                        }
                        node_evaluation += gaussian_noise * noise_magn;
                        //node evaluation is actually opposite to current evaluation
                        float winpr = 1 / (1 + Mathf.Pow(10, node_evaluation * 0.25f));
                        //upper confidencee bound
                        float ucb = Mathf.Lerp(win_probability, winpr, 1.0f / (float)(cur_visits)) + c_val * Mathf.Sqrt(Mathf.Log(last_visits) / cur_visits);
                        if(debug_this)Debug.Log("upper confidence bound for " + move.start + " to " + move.end + " is " + ucb);
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
                        UnMovePieceSimple(move);
                    }
                    int max_ind = max_inds[UnityEngine.Random.Range(0, max_inds.Count)];
                    if (current.children[max_ind] == null)
                    {
                        current.children[max_ind] = new PositionNode();
                    }
                    current = current.children[max_ind];
                    prev_nodes.Add(max_ind);
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
                        games_score += 1;
                        who_won = 1;
                        actual_mates += 1;
                    }
                    else if(black_win && !white_win)
                    {
                        games_score -= 1;
                        who_won = -1;
                        actual_mates += 1;
                    }
                }
                else
                {
                    if (white_win && !black_win)
                    {
                        games_score -= 1;
                        actual_mates += 1;
                        who_won = -1;
                    }
                    else if (black_win && !white_win)
                    {
                        games_score += 1;
                        actual_mates += 1;
                        who_won = 1;
                    }
                }
                if(debug_this) Debug.Log("rollout end, " + who_won + " to my side won");
                if(who_won != 0)
                {
                    current = node;
                    //if the next move in the tree is mine or not
                    int my_move = 1;
                    for(int p = 0; p < prev_nodes.Count; p++)
                    {
                        current = current.children[prev_nodes[p]];
                        my_move = -my_move;
                        if (my_move == who_won)
                        {
                            current.wins += 1;
                        }
                        else
                        {
                            current.wins -= 1;
                        }
                        
                    }
                }
                //if(black_win && white_win)
                //{
                //  float val_eval = ValueEvaluation();
                //   if(val_eval != 0)
                //   {
                //      bool white_more_black = ValueEvaluation() > 0;
                //     if (last_position.white_move)
                //     {
                //        if (white_more_black) games_score += 1;
                //         else games_score -= 1;
                //      }
                //      else
                //       {
                //             if (white_more_black) games_score -= 1;
                //           else games_score += 1;
                //     }
                //          }
                //     }
                previous_positions.Clear();
                previous_positions.AddRange(last_previous_positions);
                current = node;
                white_win = false;
                black_win = false;
                GetPosition(last_position);
                List<Move> lst_move = GenerateMoves(!white_move);
                legalmoves = GenerateMoves(white_move);
                if(games_score < -5)
                {
                    //this_samples = i + 1;
                    //break;
                }
            }
            games_score = (this_samples + games_score) / (float)(this_samples * 2);
            max_score = Mathf.Max(max_score, games_score);
            //maximum score
            Debug.Log(legalmove.start + " to " + legalmove.end + " : " + games_score * 100 + "%");
            GetPosition(last_position);
            previous_positions.Clear();
            previous_positions.AddRange(last_previous_positions);
            List<Move> list_move = GenerateMoves(!white_move);
            legalmoves = GenerateMoves(white_move);
            UnMovePiece(legalmove);
            games_scores[j] = games_score;

            System.GC.Collect();
        }
        search = false;
        //Debug.Log("search end");
        for(int i = 0; i < legalmoves.Count; i++)
        {
            if (games_scores[i] == max_score)
            {
                max_moves.Add(legalmoves[i]);
            }
        }
        //random max move

        Debug.Log(Time.realtimeSinceStartup - tim);
        return max_moves[UnityEngine.Random.Range(0, max_moves.Count)];
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
                                attacked_black[full]++;
                            }
                            else
                            {
                                attacked_white[full]++;
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
                                    attacked_black[current]++;
                                }
                                else
                                {
                                    attacked_white[current]++;
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
                                    attacked_black[current]++;
                                }
                                else
                                {
                                    attacked_white[current]++;
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
                                            break;
                                        }
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
                                        pinned_pieces[last_piece] = t % 2 + 3;
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
                                    attacked_black[current]++;
                                }
                                else
                                {
                                    attacked_white[current]++;
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
                                        pinned_pieces[last_piece] = t % 2 + 3;
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
                                    attacked_black[current]++;
                                }
                                else
                                {
                                    attacked_white[current]++;
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
                                attacked_black[full] ++;
                            }
                            else
                            {
                                attacked_white[full]++;
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
            if (pinned_pieces[move.start] > 0)
            {
                int pin_dir = pinned_pieces[move.start];
                int type = pieces[move.start] & 7;
                int move_dir = -1;
                int move_distance = move.end - move.start;
                if(move_distance % 7 == 0)
                {
                    move_dir = 1;
                }
                else if(move_distance % 9 == 0)
                {
                    move_dir = 2;
                }
                else if(move_distance % 8 == 0)
                {
                    move_dir = 3;
                }
                else if(move.start / 8 == move.end / 8)
                {
                    move_dir = 4;
                }
                if(move_dir != pin_dir && pin_dir!= 10)
                {
                    moves.RemoveAt(i);
                    i -= 1;
                }
                int dst = Mathf.Abs(move.start - move.end);
                if(pin_dir == 10 && type == 1 && dst != 8 && dst != 16 && (pieces[move.end] & 7) == 0)
                {
                    moves.RemoveAt(i);
                    i -= 1;
                }
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
        nodesnumbers = new int[3][];
        nodesnumbers[0] = nodes1;
        nodesnumbers[1] = nodes2;
        nodesnumbers[2] = nodes3;
        accumulators = new float[nodesnumbers.Length][];
        networks = new Layer[nodesnumbers.Length][];
        for(int i = 0; i < nodesnumbers.Length; i++)
        {
            networks[i] = new Layer[nodesnumbers[i].Length - 1];
            for (int j = 0; j < networks[i].Length; j++)
            {
                networks[i][j] = new Layer(nodesnumbers[i][j], nodesnumbers[i][j + 1]);
                //for(int t = 0; t < networks[i][j].nodesout; t++)
                //{
                 //   networks[i][j].biases[t] = UnityEngine.Random.Range(-3, 3);
                  //  for(int p = 0; p < networks[i][j].nodesin; p++)
                  //  {
                    //    networks[i][j].weights[p, t] = UnityEngine.Random.Range(-3, 3);
                    //}
                //}
            }
            accumulators[i] = new float[nodesnumbers[i][1]];
        }
        ByteToWeights(File.ReadAllBytes("Assets/networkdata"));
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
        nnue_inputs = GetInputs(false);
        Refresh_Accumulators(nnue_inputs);
        //Debug.Log(Calculate(nnue_inputs, 0)[0]);
        //Debug.Log(Calculate(nnue_inputs, 1)[0]);
        //Debug.Log(Calculate(nnue_inputs, 2)[0]);
        float evaluat = Calculate(nnue_inputs, 2)[0];
        eval_bar.SetFloat("_progress", evaluat);
        datapoints_list = new List<Datapoint>();
        //StartCoroutine(RunTraining());
    }
    IEnumerator TrainData()
    {
        //foreach(Datapoint dat in datapoints_list)
        //{
        //yield return null;
        //Datapoint[] ds = new Datapoint[1] { dat };
        float tim = Time.realtimeSinceStartup;
        //NeuralNetwork(ds, 0);
        //NeuralNetwork(ds, 1);
        //NeuralNetwork(ds, 2);
        //}
        Datapoint[] ls = datapoints_list.ToArray();
        yield return null;
        tim = Time.realtimeSinceStartup;
        NeuralNetwork(ls, 0);
        yield return null;
        tim = Time.realtimeSinceStartup;
        NeuralNetwork(ls, 1);
        yield return null;
        tim = Time.realtimeSinceStartup;
        NeuralNetwork(ls, 2);
        File.WriteAllBytes("Assets/networkdata", WeightsToByte());
    }
    int currentind = 0;
    Vector3 ogpos;
    private Move lastmove;
    private bool indinrange = false;
    private bool search = false;
    private int promo_capture = 0;
    private float[] nnue_inputs;
    
    bool paused_running = false;
    IEnumerator RunTraining()
    {
        while(!paused_running)
        {
            yield return StartCoroutine(Train_Random());
            File.WriteAllBytes("Assets/networkdata", WeightsToByte());
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Datapoint[] datapoints = new Datapoint[1];
            datapoints[0] = new Datapoint();
            datapoints[0].inputs = nnue_inputs;
            datapoints[0].outputs = new float[1] { 0.5f };
            for (int i = 0; i < 200; i++)
            {
                NeuralNetwork(datapoints, 0);
            }
            Refresh_Accumulators(nnue_inputs);
            Debug.Log(Calculate(nnue_inputs, 0)[0]);
            File.WriteAllBytes("Assets/networkdata", WeightsToByte());
        }
        if (Input.GetKey(KeyCode.O))
        {
            for(int i = 0; i < 64; i++)
            {
                if (attacked_black[i] > 0)
                {
                    highlight_squares[i].SetActive(true);
                }
            }
        }
        if(Input.GetKeyUp(KeyCode.O))
        {
            for(int i = 0; i < 64; i++)
            {
                highlight_squares[i].SetActive(false);
            }
        }
        if (Input.GetKey(KeyCode.P))
        {
            for (int i = 0; i < 64; i++)
            {
                if (attacked_white[i] > 0)
                {
                    highlight_squares[i].SetActive(true);
                }
            }
        }
        if(Input.GetKeyUp(KeyCode.P))
        {
            for (int i = 0; i < 64; i++)
            {
                highlight_squares[i].SetActive(false);
            }
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            float[] weights = new float[legalmoves.Count];
            float total_weight = 0;
            nnue_inputs = GetInputs(true);
            float current_eval = CalculateNoSigmoid(nnue_inputs, 2)[0];
            for (int t = 0; t < legalmoves.Count; t++)
            {
                Move move = legalmoves[t];
                MovePieceSimple(move);
                nnue_inputs = GetInputs(true);
                float node_evaluation = CalculateNoSigmoid(nnue_inputs, 2)[0];
                //node evaluation is actually opposite to current evaluation
                weights[t] = Mathf.Exp((-current_eval - node_evaluation) * inv_temp);
                UnMovePieceSimple(move);
                total_weight += weights[t];
            }
            float rnd_seed = UnityEngine.Random.Range(0, total_weight);
            float accum = 0;
            for (int t = 0; t < legalmoves.Count; t++)
            {
                if ((rnd_seed >= accum && rnd_seed < accum + weights[t]) || rnd_seed == total_weight)
                {
                    MovePiece(legalmoves[t]);
                    break;
                }
                accum += weights[t];
            }
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            bool forkk = ForcedWin(white_move);
            if (!forkk && !white_win && !black_win)
            {

                float[] weights = new float[legalmoves.Count];
                float total_weight = 0;
                float current_eval = 0;
                current_eval = Evaluate();
                Debug.Log("I think the position is " + current_eval);
                for (int t = 0; t < legalmoves.Count; t++)
                {
                    Move move = legalmoves[t];
                    MovePieceSimple(move);
                    float node_evaluation = Evaluate();
                    Debug.Log("I think the move from " + move.start + " to " + move.end + " is " + node_evaluation);
                    weights[t] = Mathf.Exp(Mathf.Clamp((-current_eval - node_evaluation) * inv_temp, -250, 250));
                    UnMovePieceSimple(move);
                    total_weight += weights[t];
                }
                float rnd_seed = UnityEngine.Random.Range(0, total_weight);
                float accum = 0;
                for (int t = 0; t < legalmoves.Count; t++)
                {
                    if ((rnd_seed >= accum && rnd_seed < accum + weights[t]) || rnd_seed == total_weight)
                    {
                        MovePiece(legalmoves[t]);
                        break;
                    }
                    accum += weights[t];
                }
            }
            else
            {
                Debug.Log("gameend");
            }

        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(!white_win && !black_win)
            {
                MovePiece(GenerateMove());

            }
            else
            {
                Debug.Log("gameover");
            }
            //StartCoroutine(GenerateMove());
        }
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        
        //}
        if (Input.GetKeyDown(KeyCode.Z))
        {
            UnMovePiece(lastmove);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("white to move is " + white_move);
            Debug.Log("last move was " + lastmove.start + " to " + lastmove.end);
            Debug.Log("legalmoves:");
            foreach(Move move in legalmoves)
            {
                Debug.Log(move.start + " to " + move.end);
            }
            Debug.Log("attacked black:");
            for(int i = 0; i < 64; i ++)
            {
                bool bl = attacked_black[i] > 0;
                if(bl == true) Debug.Log(i);
                if (!white_move)
                {
                    highlight_squares[i].SetActive(attacked_black[i] > 0);
                }
            }
            Debug.Log("attacked white: ");
            for (int i = 0; i < 64; i++)
            {
                bool bl = attacked_white[i] > 0;
                if (bl == true) Debug.Log(i);
                if (white_move)
                {
                    highlight_squares[i].SetActive(attacked_white[i] > 0);
                }
            }
            Debug.Log("pinned pieces: ");
            for(int i = 0; i < 64; i++)
            {
                if (pinned_pieces[i] > 0)
                {
                    Debug.Log(i);
                }
                else
                {

                }
            }
        }
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
            
            bool legal = false;
            Move tsmove = new Move();
            foreach(Move legalmove in legalmoves)
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
        foreach (GameObject square in highlight_squares)
        {
            square.SetActive(false);
        }
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
                insuf_material = true;
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
            nnue_inputs = GetInputs(true);
            float evaluat = Calculate(nnue_inputs, 2)[0];
            if (white_move)
            {
                evaluat = 1 - evaluat;
            }
            eval_bar.SetFloat("_progress", evaluat);
            Debug.Log(evaluat);
        }
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
        //if (!search)
        //{
            Render();
        //}
        List<Move> trash = GenerateMoves(!white_move);
        legalmoves = GenerateMoves(white_move);
        if(previous_positions.Count > 0)
        {
            previous_positions.RemoveAt(previous_positions.Count - 1);
        }
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
            int piece = pieces[i];
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