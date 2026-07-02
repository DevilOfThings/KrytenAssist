#!/bin/zsh

print_file() {
    if [[ -f "$1" ]]; then
        echo
        echo "============================================================"
        echo "FILE: $1"
        echo "============================================================"
        cat "$1"
    else
        echo
        echo "Missing: $1"
    fi
}

print_cs_files() {
    find "$1" \
        \( -path "*/bin" -o -path "*/obj" -o -path "*/.git" \) -prune \
        -o -name "*.cs" -print | sort | while read file
    do
        print_file "$file"
    done
}

case "$1" in

core)
    print_cs_files KrytenAssist.Core
    ;;

application)
    print_cs_files KrytenAssist.Application
    ;;

infrastructure)
    print_cs_files KrytenAssist.Infrastructure
    ;;

api)
    print_cs_files KrytenAssist.Api
    ;;

all)
    print_cs_files KrytenAssist.Core
    print_cs_files KrytenAssist.Application
    print_cs_files KrytenAssist.Infrastructure
    print_cs_files KrytenAssist.Api
    ;;

*)
    echo
    echo "Usage:"
    echo "./scripts/review.sh core"
    echo "./scripts/review.sh application"
    echo "./scripts/review.sh infrastructure"
    echo "./scripts/review.sh api"
    echo "./scripts/review.sh all"
    ;;
esac
