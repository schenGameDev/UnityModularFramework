using System;

namespace ModularFramework
{
    public class Chain<TIn, TOut>
    {
        private readonly IProcessor<TIn, TOut> _processor;

        Chain(IProcessor<TIn, TOut> processor)
        {
            _processor = processor;
        }

        public static Chain<TIn, TOut> Start(IProcessor<TIn, TOut> processor)
        {
            return new Chain<TIn, TOut>(processor);
        }
        
        public TOut Run(TIn input) => _processor.Process(input);
        public ProcessDelegate<TIn, TOut> Compile() =>  input => _processor.Process(input);
    }
    
    // each derived chain has access to its class property and method
    public abstract class Chain<TIn, TOut, TDerived> where TDerived : Chain<TIn, TOut, TDerived> 
    {
        protected readonly IProcessor<TIn, TOut> Processor;

        Chain(IProcessor<TIn, TOut> processor)
        {
            Processor = processor ?? throw new ArgumentNullException(nameof(processor));
        }

        public TNextSelf Then<TNext, TNextSelf, TProcessor>(
            TProcessor nextProcessor,
            ChainFactory<TIn, TNext, TNextSelf> factory) 
            where TNextSelf : Chain<TIn, TNext, TNextSelf> 
            where TProcessor : class, IProcessor<TOut, TNext>
        {
            if(nextProcessor == null) throw new ArgumentNullException(nameof(nextProcessor));
            if(factory == null) throw new ArgumentNullException(nameof(factory));
            
            return factory(new Combined<TIn, TOut, TNext>(Processor, nextProcessor));
        }

        public TOut Run(TIn input)
        {
            if(Processor == null) throw new ArgumentNullException(nameof(Processor));
            return  Processor.Process(input);
        }

        public ProcessDelegate<TIn, TOut> Compile()
        {
            if(Processor == null) throw new ArgumentNullException(nameof(Processor));
            return input => Processor.Process(input);
        } 
    }
    
    
    // all property should be readonly
    public interface IProcessor<in TIn, out TOut>
    {
        TOut Process(TIn input);
    }
    
    public delegate TOut ProcessDelegate<in TIn, out TOut>(TIn input);
    public delegate TChain ChainFactory<out TIn, in TOut, out TChain>(IProcessor<TIn, TOut> processor)
        where TChain : Chain<TIn, TOut, TChain>;

    internal class Combined<TA, TB, TC> : IProcessor<TA, TC>
    {
        private readonly IProcessor<TA, TB> _first;
        private readonly IProcessor<TB, TC> _second;

        public Combined(IProcessor<TA, TB> first, IProcessor<TB, TC> second)
        {
            _first = first;
            _second = second;
        }
        
        public TC Process(TA input) => _second.Process(_first.Process(input));
    }
}
