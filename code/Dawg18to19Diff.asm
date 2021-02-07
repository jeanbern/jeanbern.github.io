    L0012: mov rsi, r8
    L0015: mov rdx, [rsi]
    L0018: mov edx, [rdx+8] // load closure.word.Length
    
    // if (depth == closure.word.Length + closure.maxEdits)
    L001b: movsxd rdx, edx // expand it from 32 to 64 bit
    
    L001e: mov eax, [rsi+0x38]
    L0021: add rdx, rax // 64 bit addition
    
    L0024: movsxd r8, edi// expand depth from 32 to 64 bits

@@

	// var firstChild = closure.FirstChildEdgeIndex[currentNode];
    L003d: mov rdx, [rsi+0x28]
    
    // 
    L0041: mov r8, rdx // register move
    L0044: cmp ecx, [r8+8] // compare against FirstChildIndex.Length from memory
    L0048: jae L0302
    L004e: movsxd r9, ecx
    L0051: mov ebx, [r8+r9*4+0x10] // retrieve firstChild from memory
    
    // var lastChild = closure.FirstChildEdgeIndex[currentNode + 1];
    L0056: lea r8d, [rcx+1] // calculate currentNode + 1 to use for boundary check
    
    L005a: cmp r8d, [rdx+8] // compare calculated value against FirstChildIndex.Length from memory
    L005e: jae L0302
    
    L0064: inc ecx // increment currentNode to use as index
    
    L0066: movsxd rcx, ecx
    L0069: mov ebp, [rdx+rcx*4+0x10] // use incremented currentNode to retrieve lastChild from memory
    L006d: mov r14d, edi
    L0070: sub r14d, [rsi+0x38] // subtract closure.maxEdits from memory
    L0074: test r14d, r14d
    
@@

    // var to = Math.Min(closure.word.Length + 1, depth + closure.maxEdits + 2);
    L0087: movsxd r15, ecx // expand closure.word.Length from 32 to 64 bit
    L008a: movsxd rcx, edi // expand depth from 32 to 64 bit
    
    L008d: lea r12, [rcx+rax+2]
    L0092: cmp r15, r12
    L0095: jle short L0099
    L0097: jmp short L009c
    L0099: mov r12, r15

@@

    //
    // var previousRow = closure.matrix[depth];
    L00c6: cmp edi, [rdx+8] // boundary check with matrix.depth from memory
    
    L00c9: jae L0302
    L00cf: movsxd r8, edi
    L00d2: mov r13, [rdx+r8*8+0x10] // load previousRow
    // ++depth;
    L00d7: inc edi
    // var currentRow = closure.matrix[depth];
    L00d9: cmp edi, [rcx+8] // boundary check with matrix.depth from memory
    
@@

    // if (depth >= closure.word.Length - closure.maxEdits
    L024c: movsxd rax, eax // expand closure.word.Length from 32 to 64 bit
    L024f: mov edx, [rsi+0x38]
    
    L0252: sub rax, rdx // 64 bit subtraction from memory
    
    L0255: movsxd rdx, edi // expand depth from 32 to 64 bit
    
    L0258: cmp rax, rdx // 64 bit comparison
    L025b: jg short L02d3
