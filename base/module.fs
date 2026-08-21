: ADDR-OFFSET-GET ( addr n -- [addr + n * cell] x )
  CELLS + DUP @
;

: ADDR-OFFSET-SET ( x addr n -- [addr + n * cell] )
  CELLS + DUP !
;

: @+ ( addr -- addr+cell val )
  DUP cell+ SWAP @
;

32 CONSTANT MAX-MOD-DEP

: (CREATE-MODULE-LIST) ( pub-wid "name" -- addr )
  CREATE HERE DUP . >R , 0 , MAX-MOD-DEP CELLS ALLOT R>
;

: (MODULE-PUB-WID) ( addr -- pub-wid-addr ) ;

: (MODULE-COUNT) ( addr -- count-addr )
  1 CELLS +
;

: (MODULE-DEP-AT) ( addr i -- dep-addr )
  2 + CELLS +
;

: (MODULE-DEP-ADD) ( other-addr addr -- )
  DUP \ other-addr addr addr
  (MODULE-COUNT) \ other-addr addr count-addr
  DUP @ \ other-addr addr count-addr count
  SWAP OVER \ other-addr addr count count-addr count
  1+ DUP \ other-addr addr count count-addr count+1 count+1
  MAX-MOD-DEP \ other-addr addr count count-addr count+1 count+1 max-dep
  >= \ other-addr addr count count-addr count+1 f
  IF
    ." E1" CR ABORT
  ENDIF \ other-addr addr count count-addr count+1
  SWAP ! \ other-addr addr count
  (MODULE-DEP-AT) \ other-addr dep-addr
  !
;

: (GET-MODULES) ( addr -- ... u )
  DUP \ addr addr
  (MODULE-PUB-WID) \ addr pub-wid-addr 
  @ \ addr pub-wid
  SWAP \ pub-wid addr
  DUP \ pub-wid addr addr
  (MODULE-COUNT) \ pub-wid addr count-addr
  @ \ pub-wid addr count
  DUP \ pub-wid addr count count
  0 \ pub-wid addr count count 0
  U+DO \ pub-wid addr count
    >R \ pub-wid addr [count]
    DUP \ pub-wid addr addr [count]
    i \ pub-wid addr addr i [count]
    (MODULE-DEP-AT) \ pub-wid addr dep-addr [count]
    SWAP \ pub-wid dep-addr addr [count]
    >R \ pub-wid dep-addr [count addr]
    @
    RECURSE \ pub-wid ... u [count addr]
    R> \ pub-wid ... u addr [count]
    R> \ pub-wid ... u addr count
    ROT \ pub-wid ... addr count u
    + \ pub-wid ... addr count
  LOOP
  SWAP 
  DROP
;

: BEGIN-MODULE ( "name" -- old-wid pub-wid addr )
  GET-CURRENT \ old-wid
  WORDLIST \ old-wid pub-wid
  DUP (CREATE-MODULE-LIST) \ old-wid pub-wid addr
  WORDLIST DUP SET-CURRENT \ old-wid pub-wid addr pre-wid
  >R GET-ORDER 1+ R> SWAP SET-ORDER \ old-wid pub-wid addr
;

: END-MODULE ( old-wid pub-wid addr -- )
  2DROP SET-CURRENT GET-ORDER 1- NIP SET-ORDER
;

: IMPORT ( "name" -- u )
  GET-ORDER \ ... u
  >R    \ ... [u]
  PARSE-NAME \ ... c-addr u [u]
  FIND-NAME DUP 0= IF
    ." E2" CR ABORT
  ENDIF  \ ... xt [u]
  EXECUTE \ ... addr [u]
  (GET-MODULES) \ ... ... u [u] 
  R> \ ... ... u u
  + \ ... ... u
  DUP \ ... ... u u
  >R \ ... ... u [u]
  SET-ORDER 
  R>
;

: END-IMPORT ( u -- )
  >R GET-ORDER
  R@ -
  R> SWAP >R
  0 U+DO DROP LOOP
  R> SET-ORDER
;

: EXPORT ( pub-wid addr "name" -- pub-wid addr )
  PARSE-NAME 2DUP
  S" BEGIN-MODULE" str= IF
    2DROP
    GET-CURRENT WORDLIST DUP \ old-wid pub-wid pub-wid
    (CREATE-MODULE-LIST) OVER SET-CURRENT \ old-wid pub-wid addr
    EXIT
  ENDIF
  2DUP S" IMPORT" str= IF
    2DROP
    PARSE-NAME \ c-addr u
    FIND-NAME DUP 0= IF
      ." E3" CR ABORT
    ENDIF
    EXECUTE OVER \ addr other-addr addr
    (MODULE-DEP-ADD) \ addr
    EXIT
  ENDIF
  S" :" str= IF
    GET-CURRENT >R
    OVER SET-CURRENT
    S" : "
    [CHAR] ; PARSE
    S+
    S" ;" S+
    EVALUATE
    R> SET-CURRENT
    EXIT
  ENDIF
  ." E4" CR ABORT
;


BEGIN-MODULE T1

EXPORT : hi ." hi" ;

END-MODULE

BEGIN-MODULE T2

EXPORT : hell ." oi" ;
EXPORT IMPORT T1

END-MODULE

IMPORT T2

hi

END-IMPORT