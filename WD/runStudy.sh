#!/bin/bash

# Stop on any error
set -e

## Settings 
declare -a CLASS_CONFIGS=("ADHD-None" "ADHD-Medium" "ADHD-High" "ADHD-VeryHigh" "ADHD-None-Ambitious" "Random")
#declare -a CLASS_CONFIGS=("ClassroomConfig-ADHD-None" "ClassroomConfig-ADHD-Medium")
OUTPUT_FOLDERS=()

CLASS_CONFIG_FOLDER="./classconfigs"
SIMULATION_CONFIG="../Ressources/SimulationConfigs/SimulationConfigFile.json"
# OUTFOLDER="./ADHD_Study"
OUTFOLDER=$1
NINSTANCES=5
SEED=424242

## Generate pictures
for CC in "${CLASS_CONFIGS[@]}"
do
    echo "Running simulation model ${CC} ..."
    #python ../analysis/experiment.py ${CLASS_CONFIG_FOLDER}/${CC}.json ${OUTFOLDER}/${CC} --simulation_config_file ${SIMULATION_CONFIG} --seed ${SEED} --nInstances ${NINSTANCES} --headless --skip_agent_plots

    OUTPUT_FOLDERS+=(${OUTFOLDER}/${CC})
done

echo "Generating study results for " ${OUTPUT_FOLDERS[@]}
python ../analysis/study.py ${OUTFOLDER} ${OUTPUT_FOLDERS[@]}