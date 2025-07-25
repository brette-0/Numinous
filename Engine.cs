﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using Tomlyn;

namespace Numinous {
    internal enum Modes {
        None,
        Cartridge,
        Disk
    }

    namespace Engine {
        namespace System {
            internal enum Registers { A, X, Y }
            
            [Flags]
            internal enum Flags : byte {
                Carry       = 0x01,
                Zero        = 0x02,
            //  Interrupt   = 0x04,
            //  Decimal     = 0x08,
            //  Break       = 0x10,
            //  None (1)    = 0x20,
                Overflow    = 0x40,
                Negative    = 0x80
            }

            [Flags]
            internal enum AddressModeFlags : ushort {
                Implied             = 1 << 0,
                Immediate           = 1 << 1,
                ZeroPage            = 1 << 2,
                ZeroPageX           = 1 << 3,
                ZeroPageY           = 1 << 4,
                Absolute            = 1 << 5,
                AbsoluteX           = 1 << 6,
                AbsoluteY           = 1 << 7,
                Indirect            = 1 << 8,
                IndirectX           = 1 << 9,
                IndirectY           = 1 << 10,
                Accumulator         = 1 << 11,
                A                   = 1 << 12,
                X                   = 1 << 13,
                Y                   = 1 << 14,
                Relative            = 1 << 15,
            }

            internal static class System {
                readonly static internal Dictionary<string, AddressModeFlags> InstructionAddressModes = new() {
                    { "adc", adc }, { "and", and }, { "cmp", cmp }, { "eor", eor },
                    { "lda", lda }, { "ora", ora }, { "sta", sta },

                    { "bcc", bcc }, { "bcs", bcs }, { "bnc", bnc }, { "bns", bns },
                    { "bvc", bvc }, { "bvs", bvs }, { "bzc", bzc }, { "bzs", bzs },

                    { "bit", bit }, { "brk", brk },

                    { "clc", clc }, { "clv", clv }, { "sec", sec },
                    { "tax", tax }, { "tay", tay }, { "tsx", tsx },
                    { "txa", txa }, { "txs", txs }, { "txy", txy },

                    { "cpx", cpx }, { "cpy", cpy },

                    { "dec", dec }, { "inc", inc },

                    { "dex", dex }, { "dey", dey }, { "inx", inx }, { "iny", iny },
                    { "pha", pha }, { "php", php }, { "pla", pla }, { "plp", plp },
                    { "sei", sei }, { "rti", rti }, { "rts", rts },

                    { "jmp", jmp }, { "jsr", jsr },

                    { "ldx", ldx }, { "ldy", ldy },

                    { "asl", asl }, { "lsr", lsr }, { "rol", rol }, { "ror", ror },

                    { "nop", nop },

                    { "stx", stx }, { "sty", sty }
                };

                readonly internal static AddressModeFlags[] MemoryAddressModeInstructionTypes = [
                    // adc and cmp eor lda ora sta
                    AddressModeFlags.Immediate  |
                    AddressModeFlags.ZeroPageX  | 
                    AddressModeFlags.ZeroPageY  |
                    AddressModeFlags.Absolute   | 
                    AddressModeFlags.AbsoluteX  |
                    AddressModeFlags.AbsoluteY  | 
                    AddressModeFlags.IndirectX  | 
                    AddressModeFlags.IndirectY  ,

                    // bzc bzs bns bnc bvs bvc bcs bcc
                    AddressModeFlags.Relative   ,

                    // bit
                    AddressModeFlags.Immediate  | 
                    AddressModeFlags.ZeroPage   |
                    AddressModeFlags.Absolute   ,

                    // brk
                    AddressModeFlags.Implied    | 
                    AddressModeFlags.Immediate  ,

                    // clc clv sec tax tay tsx txa txs txy
                    AddressModeFlags.Implied    ,

                    // cpx cpy
                    AddressModeFlags.Immediate  | 
                    AddressModeFlags.ZeroPage   |
                    AddressModeFlags.Absolute   ,

                    // dec inc
                    AddressModeFlags.ZeroPage   |
                    AddressModeFlags.ZeroPageX  |
                    AddressModeFlags.Absolute   |
                    AddressModeFlags.AbsoluteX  ,

                    // dex dey inx iny pha php pla plp rti rts sei 
                    AddressModeFlags.Implied    ,

                    // jmp
                    AddressModeFlags.Absolute   | 
                    AddressModeFlags.Indirect   ,

                    // jsr 
                    AddressModeFlags.Absolute   ,

                    // ldx
                    AddressModeFlags.Immediate  |
                    AddressModeFlags.ZeroPage   |
                    AddressModeFlags.ZeroPageY  |
                    AddressModeFlags.Absolute   |
                    AddressModeFlags.AbsoluteY  ,

                    // ldy
                    AddressModeFlags.Immediate  |
                    AddressModeFlags.ZeroPage   |
                    AddressModeFlags.ZeroPageX  |
                    AddressModeFlags.Absolute   |
                    AddressModeFlags.AbsoluteX  ,

                    // asl lsr rol ror
                    AddressModeFlags.Implied    | 
                    AddressModeFlags.A          |
                    AddressModeFlags.ZeroPage   |
                    AddressModeFlags.ZeroPageX  |
                    AddressModeFlags.Absolute   |
                    AddressModeFlags.AbsoluteX  ,

                    // nop
                    AddressModeFlags.Implied    | 
                    AddressModeFlags.Immediate  | 
                    AddressModeFlags.ZeroPage   | 
                    AddressModeFlags.ZeroPageX  | 
                    AddressModeFlags.Absolute   | 
                    AddressModeFlags.AbsoluteX  ,

                    // stx
                    AddressModeFlags.ZeroPage   |
                    AddressModeFlags.ZeroPageY  |
                    AddressModeFlags.Absolute   ,

                    // sty
                    AddressModeFlags.ZeroPage   |
                    AddressModeFlags.ZeroPageX  |
                    AddressModeFlags.Absolute   ,
                ];

                readonly internal static AddressModeFlags adc = MemoryAddressModeInstructionTypes[0],
                                                          and = MemoryAddressModeInstructionTypes[0],
                                                          cmp = MemoryAddressModeInstructionTypes[0],
                                                          eor = MemoryAddressModeInstructionTypes[0],
                                                          lda = MemoryAddressModeInstructionTypes[0],
                                                          ora = MemoryAddressModeInstructionTypes[0],
                                                          sta = MemoryAddressModeInstructionTypes[0];

                readonly internal static  AddressModeFlags bcc = MemoryAddressModeInstructionTypes[1],
                                                          bcs = MemoryAddressModeInstructionTypes[1],
                                                          bnc = MemoryAddressModeInstructionTypes[1],
                                                          bns = MemoryAddressModeInstructionTypes[1],
                                                          bvc = MemoryAddressModeInstructionTypes[1],
                                                          bvs = MemoryAddressModeInstructionTypes[1],
                                                          bzc = MemoryAddressModeInstructionTypes[1],
                                                          bzs = MemoryAddressModeInstructionTypes[1];

                readonly internal static AddressModeFlags bit = MemoryAddressModeInstructionTypes[2];
                readonly internal static AddressModeFlags brk = MemoryAddressModeInstructionTypes[3];

                readonly internal static AddressModeFlags clc = MemoryAddressModeInstructionTypes[4],
                                                          clv = MemoryAddressModeInstructionTypes[4],
                                                          sec = MemoryAddressModeInstructionTypes[4],
                                                          tax = MemoryAddressModeInstructionTypes[4],
                                                          tay = MemoryAddressModeInstructionTypes[4],
                                                          tsx = MemoryAddressModeInstructionTypes[4],
                                                          txa = MemoryAddressModeInstructionTypes[4],
                                                          txs = MemoryAddressModeInstructionTypes[4],
                                                          txy = MemoryAddressModeInstructionTypes[4];

                readonly internal static AddressModeFlags cpx = MemoryAddressModeInstructionTypes[5],
                                                          cpy = MemoryAddressModeInstructionTypes[5];

                readonly internal static AddressModeFlags dec = MemoryAddressModeInstructionTypes[6],
                                                          inc = MemoryAddressModeInstructionTypes[6];

                readonly internal static AddressModeFlags dex = MemoryAddressModeInstructionTypes[7],
                                                          dey = MemoryAddressModeInstructionTypes[7],
                                                          inx = MemoryAddressModeInstructionTypes[7],
                                                          iny = MemoryAddressModeInstructionTypes[7],
                                                          pha = MemoryAddressModeInstructionTypes[7],
                                                          php = MemoryAddressModeInstructionTypes[7],
                                                          pla = MemoryAddressModeInstructionTypes[7],
                                                          plp = MemoryAddressModeInstructionTypes[7],
                                                          sei = MemoryAddressModeInstructionTypes[7],
                                                          rti = MemoryAddressModeInstructionTypes[7],
                                                          rts = MemoryAddressModeInstructionTypes[7];

                readonly internal static AddressModeFlags jmp = MemoryAddressModeInstructionTypes[8];
                readonly internal static AddressModeFlags jsr = MemoryAddressModeInstructionTypes[9];

                readonly internal static AddressModeFlags ldx = MemoryAddressModeInstructionTypes[10];
                readonly internal static AddressModeFlags ldy = MemoryAddressModeInstructionTypes[11];

                readonly internal static AddressModeFlags asl = MemoryAddressModeInstructionTypes[12],
                                                          lsr = MemoryAddressModeInstructionTypes[12],
                                                          rol = MemoryAddressModeInstructionTypes[12],
                                                          ror = MemoryAddressModeInstructionTypes[12];

                readonly internal static AddressModeFlags nop = MemoryAddressModeInstructionTypes[13];

                readonly internal static AddressModeFlags stx = MemoryAddressModeInstructionTypes[14];
                readonly internal static AddressModeFlags sty = MemoryAddressModeInstructionTypes[15];

            }
        }


        [Flags]
        internal enum WarningLevels : byte {
            IGNORE = 0x00,
            DEFAULT = 0x01,
            ERROR = 0x02,
            VERBOSE = 0x04,

            /* Internal     */
            NONE = 0xff,
            NO_OVERRULE = 0x08,

            /* Composite    */
            STRICT = VERBOSE | ERROR,
            CONTROLLED = VERBOSE | ERROR | NO_OVERRULE,

        }

        internal enum Operators : byte {
            STRING,
            FSTRING,

            OPAREN,
            CPAREN,

            OBRACK,
            CBRACK,

            OBRACE,
            CBRACE,

            DESCOPE,

            PROPERTY,
            NULLPROPERTY,

            MULT,
            DIV,
            MOD,

            ADD,
            SUB,

            RIGHT,
            LEFT,

            BITMASK,

            BITFLIP,

            BITSET,

            GT,
            LT,
            GOET,
            LOET,
            SERIAL,

            EQUAL,
            INEQUAL,

            AND,

            OR,

            NULL,
            CHECK,
            ELSE,

            SET,
            INCREASE,
            DECREASE,
            MULTIPLY,
            DIVIDE,
            MODULATE,
            NULLSET,
            RIGHTSET,
            LEFTSET,

            ASSIGNMASK,
            ASSIGNSET,
            ASSIGNFLIP,

            TERM,
            NOT,

            NONE = 255
        }

        internal enum AssembleTimeTypes : byte {
            INT,        // assemble time integer
            STRING,     // assemble time string
            
            SCOPE,      // scope type
            RT,         // Runtime Variable
            REG,        // Register
            FLAG,       // CPU Status Flag
            PROC,       // Procedure
            INTER,      // Interrupt
            BANK,       // Bank
            EXP,        // Expression

            OBJECT,     // The Boxed 'AnyType' such as long as its not constant
            COBJECT,    // The Boxed 'AnyType' clearing object reference from object, or a constant object


            CONSTANT = 0x040,

            CINT = CONSTANT,    // Constant int
            CSTRING,            // Constant string
            TYPE,               // typeof result

            CSCOPE,             // Constant Scope reference
            CRT,                // Constant runtime reference
            CREG,               // Constant register reference
            CFLAG,              // Constant flag reference
            CPROC,              // Constant procedure reference
            CINTER,             // Constant interrupt reference
            CBANK,              // Constant bank reference
            CEXP,               // Constant Expression

            IRWN,       // Indexing Register with N             foo[i + 2] situations
            ICRWN,      // Indexing Constant Register with N    foo[x + 2] situations

            FUNCTION,   // Macro Function

            MACRO = 0x80,
            // void macro
            MINT,       // int macro
            MSTRING,    // string macro
            MEXP,       // expression macro
        }

        internal enum AccessLevels : byte {
            PUBLIC = 0,
            PRIVATE = 1
        }

        internal enum AssembleTimeValueStatus : byte {
            DECLARED,   // int foo;
            PARTIAL,    // int foo = defined_later;
            OK          // int foo = 2;
        }

        internal enum ContextFetcherEnums : byte {
            OK,
            MALFORMED,
            UNTERMINATED
        }

        internal struct RunTimeVariableFilterType {
            internal uint? size;
            internal bool? signed;
            internal bool? endian;
        }

        internal struct RunTimeVariableType {
            internal uint size;     // in bytes
            internal bool signed;   // false => unsigned
            internal bool endian;   // false => little
        }

        internal enum ErrorLevels : byte {
            LOG, WARN, ERROR
        }

        internal enum ErrorTypes : byte {
            None, SyntaxError, ParsingError, NothingToDo
        }

        internal enum DecodingPhase : byte {
            TERMINAL, TOKEN, EVALUATION
        }

        internal enum Expectations : byte {
            VALUE,
            OPERATOR
        }


        internal static class Engine {
            internal static (object Return, AssembleTimeTypes Type, bool Success) Assemble(List<(object data, AssembleTimeTypes type)> args) {

                Span<List<string>>  SourceFileContentBufferSpan = CollectionsMarshal.AsSpan(Program.RegexTokenizedSourceFileContentBuffer);
                Span<string>        SourceFileNameBufferSpan    = CollectionsMarshal.AsSpan(Program.SourceFileNameBuffer);
                Span<int>           SourceSubstringBufferSpan   = CollectionsMarshal.AsSpan(Program.SourceSubstringBuffer);
                Span<int>           SourceFileLineBufferSpan    = CollectionsMarshal.AsSpan(Program.SourceFileLineBuffer);

                var CF_resp = Evaluate.ContextFetcher(ref SourceFileContentBufferSpan[^1], ref SourceSubstringBufferSpan[^1], ref SourceFileLineBufferSpan[^1], SourceFileNameBufferSpan[^1]);
                if (!CF_resp.Success) return default;

                // if its to write to ROM, ... figure that out
                // otherwise delta evaluate each step

                return default;
            }


            internal static (string filepath, bool success) CheckInclude(string target) {
                foreach (string search in Program.SourceFileSearchPaths) {
                    string fullPath = Path.Combine(AppContext.BaseDirectory, search, target);
                    if (File.Exists(fullPath)) {
                        return (fullPath, true);
                    }
                }
                return default;
            }

            /// <summary>
            /// Add Tokenized Source to Buffer
            /// </summary>
            /// <param name="FilePath"></param>
            internal static void AddSourceContext(string FilePath) {
                Program.SourceFileNameBuffer.Add(FilePath);
                Program.RegexTokenizedSourceFileContentBuffer.Add(RegexTokenize(File.ReadAllText(FilePath)));
                Program.SourceSubstringBuffer.Add(0);
                Program.SourceFileIndexBuffer.Add(0);
            }


            /// <summary>
            /// Remove the Function from the values, ensuring functions are not evaluated.
            /// By functions we refer to directives such as #include
            /// or assembler level functions such as typeof()
            /// Here we also attempt to identify if, loop or things of that nature
            /// </summary>
            /// <param name="Tokens"></param>
            /// <param name="MaxHierarchy"></param>
            internal static (object OperationContext, bool Success) ExtractTask(List<string> StringTokens) {
                // self type is not default for tokens, as they are not objects.
                if (StringTokens[0][0] == '#') {

                    if (isExplicitInstruction(StringTokens[0])) {
                        /*
                         *      ins #foo
                         *      ins foo
                         *      ins !foo
                         *      ins z:foo
                         *      ins a:foo
                         *      ins !z:foo
                         *      ins !a:foo
                         *      ins foo, r
                         *      ins !foo, r
                         *      ins z:foo, r
                         *      ins a:foo, r
                         *      ins !a:foo, r
                         *      ins foo[r]
                         *      ins z:foo[r]
                         *      ins a:foo[r]
                         *      ins !a:foo[r]
                         *      ins [foo, r]
                         *      ins [foo], r
                         */


                    }
                }



                return default;

                bool isExplicitInstruction(string ctx) {
                    switch (ctx) {
                        case "adc":

                        case "and":

                        case "asl":

                        case "bcc":
                        case "blt":

                        case "bcs":
                        case "bgt":

                        case "beq":
                        case "bzs":

                        case "bit":

                        case "bmi":
                        case "bns":

                        case "bne":
                        case "bzc":

                        case "bpl":
                        case "bnc":

                        case "brk":

                        case "bvc":

                        case "bvs":

                        case "clc":

                        case "cld":

                        case "cli":

                        case "clv":

                        case "cmp":

                        case "cpx":

                        case "cpy":

                        case "dec":

                        case "dex":

                        case "dey":

                        case "eor":

                        case "inc":

                        case "inx":

                        case "iny":

                        case "jmp":

                        case "jsr":

                        case "lda":

                        case "ldx":

                        case "ldy":

                        case "lsr":

                        case "nop":

                        case "ora":

                        case "pha":

                        case "php":

                        case "pla":

                        case "plp":

                        case "rol":

                        case "ror":

                        case "rti":

                        case "rts":

                        case "sbc":

                        case "sec":

                        case "sed":

                        case "sei":

                        case "sta":

                        case "stx":

                        case "sty":

                        case "tax":

                        case "tay":

                        case "tsx":

                        case "txa":

                        case "txs":

                        case "txy":

                        // Illegal
                        case "slo":
                        case "aso":

                        case "rla":
                        case "rln":

                        case "sre":
                        case "lse":

                        case "rra":
                        case "rrd":

                        case "sax":
                        case "aax":

                        case "lax":

                        case "dcp":
                        case "dcm":

                        case "isc":
                        case "usb":

                        case "anc":
                        case "ana":
                        case "anb":

                        case "alr":
                        case "asr":

                        case "arr":
                        case "sbx":
                        case "xma":

                        case "axs":

                        case "sha":
                        case "axa":
                        case "ahx":

                        case "shx":
                        case "sxa":
                        case "xas":

                        case "shy":
                        case "sya":
                        case "say":

                        case "tas":
                        case "shs":

                        case "las":
                        case "lar":

                        case "xaa":
                        case "ane":
                        case "axm":

                        case "stp":
                        case "kil":
                        case "hlt":
                        case "jam":

                            return true;
                        
                        default: return false;
                    }
                }

                bool isSyntheticInstruction(string ctx) {
                    switch (ctx) {
                        case "mov":
                            // mov a, x
                            // mov a, mem
                            // mov #imm, a
                            // mov $100, $200
                        case "neg":
                            // neg
                            // neg 10
                        case "abs":
                        case "ccf":
                        case "sex":
                        case "irl":
                        case "irr":
                        case "swp":
                        
                        case "rnc":
                        case "rpl":
                        case "rns":
                        case "rmi":

                        case "rcc":
                        case "rcs":
                        case "rgt":
                        case "rlt":

                        case "rvc":
                        case "rvs":

                        case "req":
                        case "rzc":
                        case "rne":
                        case "rzs":

                        case "jeq":
                        case "jzs":
                        case "jne":
                        case "jzc":

                        case "jcs":
                        case "jgt":
                        case "jcc":
                        case "jlt":

                        case "jvc":
                        case "jvs":

                        case "jns":
                        case "jmi":
                        case "jnc":
                        case "jpl":

                        case "ceq":
                        case "czs":
                        case "cne":
                        case "czc":

                        case "cmi":
                        case "cns":
                        case "cpl":
                        case "cnc":

                        case "cvs":
                        case "cvc":

                        case "ccs":
                        case "cgt":
                        case "ccc":
                        case "clt":

                        case "txy": // depend on idtable
                        case "tyx":

                        default: return false;
                    }
                }

                bool isSyntheticImplicitInstruction(string ctx) {
                    switch (ctx) {
                        default: return false;
                    }
                }

                bool isImplicitInstruction(string ctx) {
                    switch (ctx) {
                        default: return false;
                    }
                }

                bool isKeyWord(string ctx) {
                    switch (ctx) {
                        case "using":
                            // using math
                            // using m = math
                            // using math.add
                            // using add = math.add

                        case "if":
                            // if, if else, if elseif, if elseif else

                        case "elseif":
                            // elseif, elseif else

                        case "else":
                            // else

                        case "loop":
                            // loop, break

                        case "break":
                            // break

                        case "return":
                            // return
                            // return foo, bar
                            break;

                        case "enum":
                            // enum alias {contents}

                        case "bank":
                            // bank operation is complicated

                        case "proc":
                            // proc alias { body }

                        case "charmap":
                            // charmap  {a = b}


                        // case struct
                        
                        default: return false;
                    }
                    return true;
                }
            }

            



            // Generated Function | However I do find that this function is how I would code and meets criteria
            internal static Dictionary<TKey, TValue> Clone<TKey, TValue>(Dictionary<TKey, TValue> Source) where TKey : notnull {
                var clone = new Dictionary<TKey, TValue>(Source.Count);
                foreach (var kv in Source) {
                    var keyClone = Clone(kv.Key);
                    var valueClone = Clone(kv.Value);
                    clone[keyClone] = valueClone;
                }
                return clone;
            }

            internal static T Clone<T>(T ctx) => ctx switch {
                ICloneable c => (T)c.Clone(),
                string or ValueType => ctx,
#if DEBUG
                _ => throw new NotSupportedException($"Cannot clone type {ctx?.GetType()}")
#else
                _ => throw new NotSupportedException($"FATAL ERROR :: (REPORT THIS ON THE GITHUB) CANNOT CLONE TYPE {ctx?.GetType()}")
#endif
            };
            internal enum Unary : byte {
                INC,
                DEC,
                ABS,
                NEG,
                BIT,
                NOT
            };

            internal static bool IsNonLiteral(char First) =>
                    First switch {
                        '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9' or '$' or '%' or '&' or '+' or '-' or '!' or '^' or '*' or '[' or ']' or '{' or '}' or '\'' or '#' or '~' or ':' or ',' or '<' or '.' or '>' or '/' or '?' => false,
                        _ => true,
                    };


            internal static readonly string[] Reserved = [
                // directives
                "cpu", "rom", "define", "undefine", "include", "assert",
                
                // assemble time types
                "ref", "void", "int", "string", "exp", "bank", "proc", "reg", "flag", "scope", "namespace",
                
                // conditional assembly
                "if", "else", "loop", "break", "return",
                
                // runtime type filters
                "ux", "ix", "lx", "bx", "ulx", "ilx", "ubx", "ibx", "num", 
                "x8", "x16", "x24", "x32", "x64", "l16", "l24", "l32", "l64", "b16", "b24", "b32", "b64",
            
                // runtime type memory adjectives
                "direct", "system", "program", "mapper", "field", "slow", "fast",

                // runtime types
                "u8", "i8", "u16", "i16", "u24", "i24", "u32", "i32", "u64", "i64",
                "ub16", "ib16", "ub24", "ib24", "ub32", "ib32", "ub64", "ib64",

                // Operators
                // Assignment
                "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", ">>=", "<<=",

                // Unary
                "==", "!=", "<=", ">=", ">", "<",

                // LLambda
                "=>",

                // Null Coalescence
                "??", "??=", "?.",

                // Math
                "+", "-", "*", "/", "%", "&", "|", "^", "~", "(", ")",

                // Indexing
                ",", "[", "]",

                // Numerical Systems
                "%", "$", "£", "0x", "0b", "0o",

                // Control
                ";", ":", "#", "\\", "\"", "{", "}", "?", ">", "<", "!", ".", ","
            ];

            internal static List<string> ResolveDefines(List<string> tokens) {
                bool DidReplace;

                do {
                    DidReplace = false;
                    List<string> UpdatedTokens = [];

                    for (int i = 0; i < tokens.Count; i++) {
                        string token = tokens[i];
                        ((object data, AssembleTimeTypes type, AccessLevels access) ctx, bool success) = Database.GetObjectFromAlias(token, AccessLevels.PUBLIC);
                        if (success && ctx.type == AssembleTimeTypes.CSTRING) {
                            var foo = (Dictionary<string, (object data, AssembleTimeTypes type, AccessLevels access)>)ctx.data;
                            var bar = foo[""].data;
                            string Capture = (string)bar;
                            UpdatedTokens.AddRange(RegexTokenize(Capture));
                            DidReplace = true;
                        } else UpdatedTokens.Add(token);
                    }
                    
                    tokens = UpdatedTokens;
                } while (DidReplace);
                return tokens;
            }

            internal static class Terminal {
                internal enum Responses : byte {
                    Terminate_Error,
                    Terminate_Success,
                    Proceed,
                }
                
                
                internal static (string InputPath, string OutputPath, Responses Response) Parse(string[] args) {
                    string InputPath = "", OutputPath = "";
                    int StringIndex = 0;
                    string Flattened = string.Join(" ", args);

                    Responses Response = Responses.Proceed;
                    Program.WarningLevel = WarningLevels.NONE;

                    for (int i = 0; i < args.Length; i++) {
                        StringIndex += args[i].Length;

                        switch (args[i]) {
                            case "-i":
                            case "--input":
                                if (i == args.Length - 1) {
                                    Error(ErrorTypes.ParsingError, DecodingPhase.TERMINAL, $"{(Language.Language.Connectives[(Program.ActiveLanguage, "No Input Path Provided")])}.", -1, default, ApplyWiggle(Flattened, StringIndex, args[i].Length));
                                    return default;
                                } else if (InputPath.Length > 0) {
                                    Error(ErrorTypes.ParsingError, DecodingPhase.TERMINAL, $"{(Language.Language.Connectives[(Program.ActiveLanguage, "Input Source File Path has already been specified")])}.", -1, default, ApplyWiggle(Flattened, StringIndex, args[i].Length));
                                    return default;
                                } else {
                                    InputPath = args[++i];
                                }
                                break;

                            case "-o":
                            case "--output":
                                if (i == args.Length - 1) {
                                    Error(ErrorTypes.ParsingError, DecodingPhase.TERMINAL, $"{(Language.Language.Connectives[(Program.ActiveLanguage, "No Output Path Provided")])}.", -1, default, ApplyWiggle(Flattened, StringIndex, args[i].Length));
                                    return default;
                                } else if (OutputPath.Length > 0) {
                                    Error(ErrorTypes.ParsingError, DecodingPhase.TERMINAL, $"{(Language.Language.Connectives[(Program.ActiveLanguage, "Output Binary File Path has already been specified")])}.", -1, default, ApplyWiggle(Flattened, StringIndex, args[i].Length));
                                    return default;
                                }
                                OutputPath = args[++i];
                                break;

                            case "-w":
                            case "--warning":
                                if (i == args.Length - 1) {
                                    // error, no warning description detected
                                    return default;
                                } else if (Program.WarningLevel != WarningLevels.NONE) {
                                    // error, already described warning level
                                    return default;
                                }
                                
                                Program.WarningLevel = args[++i] switch {
                                    "i" or "ignore"     or "I" or "IGNORE"      => WarningLevels.IGNORE,
                                    "d" or "default"    or "D" or "DEFAULT"     => WarningLevels.DEFAULT,
                                    "e" or "error"      or "E" or "ERROR"       => WarningLevels.ERROR,
                                    "v" or "verbose"    or "V" or "VERBOSE"     => WarningLevels.VERBOSE,
                                    "s" or "strict"     or "S" or "STRICT"      => WarningLevels.STRICT,
                                    "c" or "controlled" or "C" or "CONTROLLED"  => WarningLevels.CONTROLLED,

                                    _ => WarningLevels.NONE
                                };

                                if (Program.WarningLevel == WarningLevels.NONE) {
                                    // error : unrecognized warning level 
                                    return default;
                                }
                                break;

                            case "-h":
                            case "--help":
                                Response = Responses.Terminate_Success;

                                if (i == args.Length - 1) {
                                    // generic help message
                                    Log(ErrorTypes.None, DecodingPhase.TERMINAL,
$"""
Numinous 2a03 - GPL V2 Brette Allen 2026

-i | --input        | [path]    | {(Language.Language.Connectives[(Program.ActiveLanguage, "Entrypoint Source Assembly File")])}
-o | --output       | [path]    | {(Language.Language.Connectives[(Program.ActiveLanguage, "Output ROM/Disk Binary Output")])}
-h | --help         |           | {(Language.Language.Connectives[(Program.ActiveLanguage, "Display the help string (you did that)")])}
-h | --help         | [arg]     | TODO: WRITE "GET INFO ON SPECIFIC ARGUMENT FUNCTION" HERE
-l | --language     | [lang]    | {(Language.Language.Connectives[(Program.ActiveLanguage, "Choose a language to use")])}
-w | --warning      | [level]   | TODO: Write "SET WARNING LEVEL" HERE
       
""", -1, default, null);
                                } else {
                                    switch (args[++i]) {
                                        default: --i; break;

                                        case "l":
                                        case "lang":
                                        case "languages":
                                            // language specific help message.
                                            Log(ErrorTypes.None, DecodingPhase.TERMINAL, @"
English (UK)      ""-l en_gb""
English (US)      ""-l en_us""
Español           ""-l es""
Deutsch           ""-l de""
日本語            ""-l ja""
Français          ""-l fr""
Português         ""-l pt""
Русский           ""-l ru""
Italiano          ""-l it""
Nederlands        ""-l ne""
Polski            ""-l pl""
Türkçe            ""-l tr""
Tiếng Việt        ""-l vt""
Bahasa Indonesia  ""-l in""
Čeština           ""-l cz""
한국어            ""-l ko""
Українська        ""-l uk""
العربية           ""-l ar""
Svenska           ""-l sw""
فارسی             ""-l pe""
中文              ""-l ch""
", -1, default, null);
                                            break;

                                        case "w":
                                        case "warn":
                                        case "warnings":
                                            // warnings specific help message
                                            Log(ErrorTypes.None, DecodingPhase.TERMINAL,
                                            $"""
Numinous Warning Types and how they work

ignore      : Will not display any warnings, but track the quantity for after completion.
default     : Will warn the user about potential issues with their code.
error       : Will convert all errors into warnings, enforcing the user to fix all issues.
verbose     : Will display much more warnings, recommended and intended for those who wish to write perfect code.
strict      : Acts as 'verbose' but warnings become errors, not recommended.
controlled  : Acts as 'strict' but prevents overruling.
       
""", -1, default, null);
                                            break;

                                        case "i":
                                        case "input":
                                            Log(ErrorTypes.None, DecodingPhase.TERMINAL,
$"""
Numinous Input File

The input file argument (-i or --input) should be followed by a valid file path to a source assembly file. 
If the file is empty you will receive an error, you may only pass one file here as the entry point file.
This decides what the root of the "include path" is, includes from here must be relative to this path.
       
""", -1, default, null);
                                            break;

                                        case "o":
                                        case "output":
                                            Log(ErrorTypes.None, DecodingPhase.TERMINAL,
$"""
Numinous Output File

The output file argument (-o or --output) should be followed by a path pointing to a file to generate.
The file name must comply with the limits of your Operating System.
The directory the output file lives in must also already exist. 
If you wish to create an FDS Disk image, you must use the FDS Header variant as using the *.fds file extension
will not affect the kind of build produced. 

Numinous WILL overwrite a file existing with the same name at the output path if found.
       
""", -1, default, null);
                                            break;
                                    }
                                }


                                break;

                            case "-l":
                            case "--language":
                                if (i == args.Length - 1) {
                                    Error(ErrorTypes.ParsingError, DecodingPhase.TERMINAL, $"{(Language.Language.Connectives[(Program.ActiveLanguage, "No Language Provided")])}.", -1, default, ApplyWiggle(Flattened, StringIndex, args[i].Length));
                                    return default;
                                }

                                Program.ActiveLanguage = args[++i] switch {
                                    "en_gb" => Language.Languages.English_UK,
                                    "en_us" => Language.Languages.English_US,
                                    "es"    => Language.Languages.Spanish,
                                    "de"    => Language.Languages.German,
                                    "ja"    => Language.Languages.Japanese,
                                    "fr"    => Language.Languages.French,
                                    "pt"    => Language.Languages.Portuguese,
                                    "ru"    => Language.Languages.Russian,
                                    "it"    => Language.Languages.Italian,
                                    "ne"    => Language.Languages.Dutch,
                                    "pl"    => Language.Languages.Polish,
                                    "tr"    => Language.Languages.Turkish,
                                    "vt"    => Language.Languages.Vietnamese,
                                    "in"    => Language.Languages.Indonesian,
                                    "cz"    => Language.Languages.Czech,
                                    "ko"    => Language.Languages.Korean,
                                    "uk"    => Language.Languages.Ukrainian,
                                    "ar"    => Language.Languages.Arabic,
                                    "sw"    => Language.Languages.Swedish,
                                    "pe"    => Language.Languages.Persian,
                                    "ch"    => Language.Languages.Chinese,

                                    _       => Language.Languages.Null
                                };

                                if (Program.ActiveLanguage == Language.Languages.Null) {
                                    Error(ErrorTypes.ParsingError, DecodingPhase.TERMINAL, $"{(Language.Language.Connectives[(Program.ActiveLanguage, "Invalid Language Provided")])}.", -1, default, ApplyWiggle(Flattened, StringIndex, args[i].Length));
                                    return default;
                                }
                                break;

                            default:
                                Error(ErrorTypes.ParsingError, DecodingPhase.TERMINAL, $"{(Language.Language.Connectives[(Program.ActiveLanguage, "Unrecognized Terminal Argument")])}.", -1, default, ApplyWiggle(Flattened, 1 + StringIndex, args[i].Length));
                                return default;
                        }
                    }

                    return LoadConfig() ? (InputPath, OutputPath, Response) : default;

                    static bool LoadConfig() {
                        if (!File.Exists($"{AppContext.BaseDirectory}/Numinous.toml")) {
                            File.WriteAllText($"{AppContext.BaseDirectory}/Numinous.toml", """
[Defaults]
DefaultLanguage             = "System"
DefaultWarningLevel         = "Default"

[Paths]
LibraryIncludePaths         = ["./libs"]
""");
                        }

                        var Config = Toml.ToModel<NuminousConfigTomlTemplate>(
                            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Numinous.toml")),
                            null,
                            new TomlModelOptions { ConvertPropertyName = name => name }
                        );

                        #region Warning level from Config TOML
                        if (Program.WarningLevel == WarningLevels.NONE) Program.WarningLevel = Config.Defaults.DefaultWarningLevel switch {
                            "Ignore"        => WarningLevels.IGNORE,
                            "Default"       => WarningLevels.DEFAULT,
                            "Error"         => WarningLevels.ERROR,
                            "Verbose"       => WarningLevels.VERBOSE,
                            "Strict"        => WarningLevels.STRICT,
                            "Controlled"    => WarningLevels.CONTROLLED, 

                            _               => WarningLevels.NONE // mark to fix toml
                        };
                       
                        if (Program.WarningLevel == WarningLevels.NONE) {
                            Warn(ErrorTypes.SyntaxError, DecodingPhase.TERMINAL, $"""
The config file (at {AppContext.BaseDirectory}/Numinous.toml) is malformed! 
Ensure that it contains the key 'DefaultWarningLevel' under 'Defaults' table. The data may be any of the following:

Ignore                  : By default will ignore all warnings, great for sloppy vibe coding with minimal output.
Default                 : Provides few errors and doesn't halt your workflow
Error                   : Treats warning as errors, not recommended but does enforce clean code.
Verbose                 : Shows more warnings, even those which are harmless.
Strict                  : Shows more warnings as errors, not recommended but does enforce clean code.
Controlled              : Functions like Strict but prevents use of overrides. 

Project Numinous will NOT continue until you fix this or manually specify your Warning Level!
""", default, default, default);
                            return false;
                        }
                        #endregion Warning level from Config TOML

                        #region Default Langauge from Config TOML
                        if (Program.ActiveLanguage == Language.Languages.Null) Program.ActiveLanguage = Config.Defaults.DefaultLanguage switch {
                            "English UK"    => Language.Languages.English_UK,
                            "English US"    => Language.Languages.English_US,
                            "Spanish"       => Language.Languages.Spanish,
                            "German"        => Language.Languages.German,
                            "Japanese"      => Language.Languages.Japanese,
                            "French"        => Language.Languages.French,
                            "Portuguese"    => Language.Languages.Portuguese,
                            "Russian"       => Language.Languages.Russian,
                            "Italian"       => Language.Languages.Italian,
                            "Dutch"         => Language.Languages.Dutch,
                            "Polish"        => Language.Languages.Polish,
                            "Turkish"       => Language.Languages.Turkish,
                            "Vietnamese"    => Language.Languages.Vietnamese,
                            "Indonesian"    => Language.Languages.Indonesian,
                            "Czech"         => Language.Languages.Czech,
                            "Korean"        => Language.Languages.Korean,
                            "Ukrainian"     => Language.Languages.Ukrainian,
                            "Arabic"        => Language.Languages.Arabic,
                            "Swedish"       => Language.Languages.Swedish,
                            "Persian"       => Language.Languages.Persian,
                            "Chinese"       => Language.Languages.Chinese,

                            "System"        => Language.Language.CaptureSystemLanguage(),
                            _               => Language.Languages.Null
                        };

                        if (Program.ActiveLanguage == Language.Languages.Null) {
                            Warn(ErrorTypes.SyntaxError, DecodingPhase.TERMINAL, $"""
The config file (at {AppContext.BaseDirectory}/Numinous.toml) is malformed! 
Ensure that it contains the key 'DefaultLanguage' under 'Defaults' table. The data may be any of the following:

English UK
English US
Spanish
German
Japanese
French
Portuguese
Russian
Italian
Dutch
Polish
Turkish
Vietnamese
Indonesian
Czech
Korean
Ukrainian
Arabic
Swedish
Persian
Chinese

Project Numinous will NOT continue until you fix this or manually specify your language!
""", default, default, default);
                            return false;
                        }
                        #endregion Default Langauge from Config TOML

                        Program.SourceFileSearchPaths = [.. Config.Paths.LibraryIncludePaths];

                        if (Program.SourceFileSearchPaths.Count == 0) {
                            // warn, no libraries at all (this is unusual, they should at least have the standard library)
                            return false;
                        }

                        return true;
                    }
                }

                internal class NuminousConfigTomlTemplate {
                    public class DefaultsBlock {
                        public string DefaultWarningLevel { get; set; } = "DefaultWarningLevel";
                        public string DefaultLanguage { get; set; } = "DefaultLanguage";
                    }

                    public class PathsBlock {
                        public string[] LibraryIncludePaths { get; set; } = [];
                    }

                    public PathsBlock    Paths    { get; set; } = new();
                    public DefaultsBlock Defaults { get; set; } = new();
                }

                // in event of left in message, don't show on release
#if DEBUG
                internal static void Debug(string message) => Console.WriteLine(message);
#else
                internal static void debug() {}
#endif

#if DEBUG
                internal static void WriteInfo(ErrorLevels ErrorLevel, ErrorTypes ErrorType, DecodingPhase Phase, string Message, int LineNumber, int StepNumber, string? Context,
                    int     lineNumber = 0, 
                    string  filePath = "", 
                    string  memberName = "")
#else
                internal static void WriteInfo(ErrorLevels ErrorLevel, ErrorTypes ErrorType, DecodingPhase Phase, string Message, int? LineNumber, int? StepNumber, string? Context) 
#endif
                {
                    Console.ForegroundColor = ErrorLevel switch {
                        ErrorLevels.LOG     => ConsoleColor.Cyan, 
                        ErrorLevels.WARN    => ConsoleColor.Yellow, 
                        ErrorLevels.ERROR   => ConsoleColor.Red, 
                        
                        _                   => ConsoleColor.White
                    };

                    string ErrorTypeString, ErrorTypeConnective, LocationString, DecodePhaseString;

                    if (ErrorType  == ErrorTypes.None) {
                        Console.WriteLine(Message);
                        goto Exit;
                    }

                    ErrorTypeString     = Language.Language.ErrorTypeMessages[(Program.ActiveLanguage, ErrorType)];
                    ErrorTypeConnective = Language.Language.Connectives[(Program.ActiveLanguage, "During")];
                    DecodePhaseString   = Language.Language.DecodePhaseMessages[(Program.ActiveLanguage, Phase)];
                    LocationString      = LineNumber == -1 ? "" : (StepNumber == 0 ? $"({LineNumber})" : $"({LineNumber}, {StepNumber})");
                    Context = Context == null ? "" : $": {Context}";

                    // Something Error During Something Phase :: Could not do a thing (1, 2) : ah, the issue is here.
#if DEBUG
                    Console.WriteLine($"{ErrorTypeString} {ErrorTypeConnective} {DecodePhaseString} :: {Message} {Program.SourceFileNameBuffer[^1]} {LocationString}{Context}");
                    Console.WriteLine($"[{filePath}:{lineNumber}] {memberName}");
#else
                    Console.WriteLine($"{ErrorTypeString} {ErrorTypeConnective} {DecodePhaseString} :: {Message} {LocationString}{Context}");
#endif

                Exit:
                    Console.ResetColor();
                }

#if DEBUG
                    
                internal static void   Log(ErrorTypes ErrorType, DecodingPhase Phase, string Message, int LineNumber, int StepNumber, string? Context,
                    [CallerLineNumber] int lineNumber = 0,
                    [CallerFilePath] string filePath = "",
                    [CallerMemberName] string memberName = "") => WriteInfo(ErrorLevels.LOG,   ErrorType, Phase, Message, LineNumber, StepNumber, Context, lineNumber, filePath, memberName);
                

                internal static void  Warn(ErrorTypes ErrorType, DecodingPhase Phase, string Message, int LineNumber, int StepNumber, string? Context,
                    [CallerLineNumber] int lineNumber = 0,
                    [CallerFilePath] string filePath = "",
                    [CallerMemberName] string memberName = "") => WriteInfo(Program.WarningLevel.HasFlag(WarningLevels.ERROR) ? ErrorLevels.ERROR : ErrorLevels.WARN,  ErrorType, Phase, Message, LineNumber, StepNumber, Context, lineNumber, filePath, memberName);


                internal static void Error(ErrorTypes ErrorType, DecodingPhase Phase, string Message, int LineNumber, int StepNumber, string? Context,
                    [CallerLineNumber] int lineNumber = 0,
                    [CallerFilePath] string filePath = "",
                    [CallerMemberName] string memberName = "") => WriteInfo(ErrorLevels.ERROR, ErrorType, Phase, Message, LineNumber, StepNumber, Context, lineNumber, filePath, memberName);

#else
                internal static void   Log(ErrorTypes ErrorType, DecodingPhase Phase, string Message, int? LineNumber, int? StepNumber, string? Context) {
                    WriteInfo(ErrorLevels.LOG,   ErrorType, Phase, Message, LineNumber, StepNumber, Context);
                }

                internal static void  Warn(ErrorTypes ErrorType, DecodingPhase Phase, string Message, int? LineNumber, int? StepNumber, string? Context) {
                    WriteInfo(ErrorLevels.WARN,  ErrorType, Phase, Message, LineNumber, StepNumber, Context);
                }

                internal static void Error(ErrorTypes ErrorType, DecodingPhase Phase, string Message, int? LineNumber, int? StepNumber, string? Context) {
                    WriteInfo(ErrorLevels.ERROR, ErrorType, Phase, Message, LineNumber, StepNumber, Context);
                }
#endif
            }

            /// <summary>
            /// Searches for 'Alias' in TargetScope (Either ActiveScope or specified scope)
            /// </summary>
            /// <param name="Alias"></param>
            /// <param name="TargetScope"></param>
            /// <param name="UsedAccessLevel"></param>
            /// <returns></returns>
            internal static ((object data, AssembleTimeTypes type, AccessLevels access) ctx, bool success) GetObjectFromAlias(string Alias, Dictionary<string, (object data, AssembleTimeTypes type, AccessLevels access)> TargetScope, AccessLevels UsedAccessLevel) {
                ((object data, AssembleTimeTypes type, AccessLevels access) ctx, bool found, bool error) = (default, default, default);
                List<Dictionary<string, (object data, AssembleTimeTypes type, AccessLevels access)>> LocalObjectSearchBuffer;

                if (TargetScope == Program.ActiveScopeBuffer[^1]) {
                        LocalObjectSearchBuffer = [.. Program.ObjectSearchBuffer, Program.ActiveScopeBuffer[^1]];
                } else  LocalObjectSearchBuffer = [TargetScope]; 


                if (!LocalObjectSearchBuffer.Contains(TargetScope)) LocalObjectSearchBuffer.Add(TargetScope);

                foreach (var LocalObjectSearchContainer in LocalObjectSearchBuffer) {
                    if (LocalObjectSearchContainer.TryGetValue(Alias, out ctx)) {
                        if (UsedAccessLevel < ctx.access) {
                            // error, invalid permissions to access item
                            return default;
                        } else return (ctx, true);
                    }
                }

                return default;
            }

            /// <summary>
            /// Database methods
            /// </summary>
            internal static class Database {
                /// <summary>
                /// Get without specifying target scope.
                /// 
                /// Order may be changed depending on Program.ObjectSearchBuffer. The ActiveScope is ALWAYS searched first, afterwards its down to this.
                /// By default the ObjectSearchBuffer only includes the root scope.
                /// </summary>
                /// <param name="Alias"></param>
                /// <param name="UsedAccessLevel"></param>
                /// <returns></returns>
                internal static ((object data, AssembleTimeTypes type, AccessLevels access) ctx, bool success) GetObjectFromAlias(string Alias, AccessLevels UsedAccessLevel) {
                    List<Dictionary<string, (object data, AssembleTimeTypes type, AccessLevels access)>> LocalObjectSearchBuffer = [Program.ActiveScopeBuffer[^1], .. Program.ObjectSearchBuffer];
                    return __GetObjectFromAlias(Alias, LocalObjectSearchBuffer, UsedAccessLevel);
                }
                /// <summary>
                /// Only check the specified scope, may be used like rs\foo. Note that the scope used to specify will be the result of the other method being used first.
                /// After this its hierarchy based and therefore rs\foo\foo may not always work.
                /// </summary>
                /// <param name="Alias"></param>
                /// <param name="TargetScope"></param>
                /// <param name="UsedAccessLevel"></param>
                /// <returns></returns>
                /// 
                internal static ((object data, AssembleTimeTypes type, AccessLevels access) ctx, bool success) GetObjectFromAlias(string Alias, Dictionary<string, (object data, AssembleTimeTypes type, AccessLevels access)> TargetScope, AccessLevels UsedAccessLevel) => __GetObjectFromAlias(Alias, [TargetScope], UsedAccessLevel);
                
                /// <summary>
                /// Internal function iterating over the LocalObjectSearchPath to find the required context if possible.
                /// </summary>
                /// <param name="Alias"></param>
                /// <param name="LocalObjectSearchBuffer"></param>
                /// <param name="UsedAccessLevel"></param>
                /// <returns></returns>
                private  static ((object data, AssembleTimeTypes type, AccessLevels access) ctx, bool success) __GetObjectFromAlias(string Alias, List<Dictionary<string, (object data, AssembleTimeTypes type, AccessLevels access)>> LocalObjectSearchBuffer, AccessLevels UsedAccessLevel) {
                    ((object data, AssembleTimeTypes type, AccessLevels access) ctx, bool found, bool error) = (default, default, default);
                    foreach (var LocalObjectSearchContainer in LocalObjectSearchBuffer) {
                        if (LocalObjectSearchContainer.TryGetValue(Alias, out ctx)) {
                            if (UsedAccessLevel < ctx.access) {
                                // error, invalid permissions to access item
                                return default;
                            } else return (ctx, true);
                        }
                    }

                    return default;
                }
            }

            internal static string ApplyWiggle(string input, int start, int length) {
                const char wiggle = '\u0330';
                var builder = new StringBuilder(input.Length * 2);

                for (int i = 0; i < input.Length; i++) {
                    builder.Append(input[i]);
                    if (i >= start && i < start + length)
                        builder.Append(wiggle);
                }

                return builder.ToString();
            }


            // Generated function : I don't know how regex works
            /// <summary>
            /// Tokenizes a line of code. SPACES ARE IMPORTANT FOR LINE INDEX MATH
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            /// <summary>
            /// Tokenizes a line of code. SPACES ARE IMPORTANT FOR LINE INDEX MATH
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            internal static List<string> RegexTokenize(string input) {
                // Wide multi-character operators and atomic tokens, now including comment tokens
                string[] atomicTokens = new[] {
                    "//", "/*", "*/",
                    "$\"", "<=>", "==", "!=", "<=", ">=", "&&", "||", "++", "--",
                    "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<", ">>", "->", "??", "?."
                };

                // Escape them for regex and order by length
                string escapedTokens = string.Join("|", atomicTokens
                    .Select(Regex.Escape)
                    .OrderByDescending(s => s.Length));

                // Single-character separators including whitespace
                string separators = @"!""£$%\^&*()+\-=\[\]{};:'@#~\\|,<.>/?\s";

                // Final pattern:
                // 1. Match atomic tokens
                // 2. Match non-separator sequences
                // 3. Match single separators
                string pattern = $@"({escapedTokens})|([^{separators}]+)|([{separators}])";

                var matches = Regex.Matches(input, pattern);
                var tokens = new List<string>();

                foreach (Match match in matches) {
                    if (!string.IsNullOrEmpty(match.Value))
                        tokens.Add(match.Value);
                }

                return tokens;
            }
        }
    }
}